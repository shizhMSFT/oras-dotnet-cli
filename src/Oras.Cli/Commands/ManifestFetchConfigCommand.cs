using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Manifest fetch-config command - fetch the config blob referenced by a manifest.
/// </summary>
internal static class ManifestFetchConfigCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("fetch-config", "Fetch the config blob referenced by a manifest");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Manifest reference (registry/repository:tag or @digest)"
        };
        command.Add(referenceArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add platform options
        var platformOptions = new PlatformOptions();
        platformOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        // Add output file option
        var outputOpt = new Option<string?>("--output", "-o")
        {
            Description = "Write to file instead of stdout",
            DefaultValueFactory = _ => null
        };
        command.Add(outputOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var output = parseResult.GetValue(outputOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                var repo = await registryService.CreateRepositoryAsync(
                    reference,
                    username,
                    password,
                    plainHttp,
                    insecure,
                    cancellationToken).ConfigureAwait(false);

                var resolveRef = ReferenceHelper.ExtractDigest(reference) ?? ReferenceHelper.ExtractTag(reference) ?? "latest";

                // Step 1: Fetch the manifest
                var (manifestDescriptor, manifestStream) = await repo.Manifests.FetchAsync(resolveRef, cancellationToken).ConfigureAwait(false);
                string manifestJson;
                await using (manifestStream)
                {
                    using var reader = new StreamReader(manifestStream);
                    manifestJson = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                }

                // Step 2: Parse manifest to extract config descriptor
                using var doc = System.Text.Json.JsonDocument.Parse(manifestJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("config", out var configElement))
                {
                    throw new OrasException("Manifest does not contain a config field",
                        "This manifest may not be an OCI image manifest.");
                }

                var configDigest = configElement.GetProperty("digest").GetString()!;
                var configMediaType = configElement.GetProperty("mediaType").GetString() ?? "application/octet-stream";
                var configSize = configElement.GetProperty("size").GetInt64();

                var configDescriptor = new OrasProject.Oras.Oci.Descriptor
                {
                    MediaType = configMediaType,
                    Digest = configDigest,
                    Size = configSize
                };

                // Step 3: Fetch config blob
                var configStream = await repo.Blobs.FetchAsync(configDescriptor, cancellationToken).ConfigureAwait(false);
                string configJson;
                await using (configStream)
                {
                    using var reader = new StreamReader(configStream);
                    configJson = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                }

                // Step 4: Output
                if (output != null)
                {
                    await File.WriteAllTextAsync(output, configJson, cancellationToken).ConfigureAwait(false);
                    AnsiConsole.MarkupLine($"[green]✓[/] Saved config to {Markup.Escape(output)}");
                }
                else
                {
                    formatter.WriteJson(configJson, pretty: true);
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
