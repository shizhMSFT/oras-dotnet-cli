using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Manifest fetch command - fetch a manifest from a registry.
/// </summary>
internal static class ManifestFetchCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("fetch", "Fetch a manifest from a registry");

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

        // Add descriptor mode option
        var descriptorOpt = new Option<bool>("--descriptor")
        {
            Description = "Output descriptor only (not full manifest)",
            DefaultValueFactory = _ => false
        };
        command.Add(descriptorOpt);

        // Add output file option
        var outputOpt = new Option<string?>("--output", "-o")
        {
            Description = "Write to file instead of stdout",
            DefaultValueFactory = _ => null
        };
        command.Add(outputOpt);

        // Add pretty print option
        var prettyOpt = new Option<bool>("--pretty")
        {
            Description = "Pretty-print JSON output",
            DefaultValueFactory = _ => false
        };
        command.Add(prettyOpt);

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
                var descriptorMode = parseResult.GetValue(descriptorOpt);
                var output = parseResult.GetValue(outputOpt);
                var pretty = parseResult.GetValue(prettyOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                var repo = await registryService.CreateRepositoryAsync(
                    reference,
                    username,
                    password,
                    plainHttp,
                    insecure,
                    cancellationToken).ConfigureAwait(false);

                var tag = ReferenceHelper.ExtractTag(reference);
                var digest = ReferenceHelper.ExtractDigest(reference);
                var resolveRef = digest ?? tag ?? "latest";

                if (descriptorMode)
                {
                    var descriptor = await repo.Manifests.ResolveAsync(resolveRef, cancellationToken).ConfigureAwait(false);
                    formatter.WriteDescriptor(new DescriptorResult(
                        descriptor.MediaType,
                        descriptor.Digest,
                        descriptor.Size,
                        descriptor.Annotations as Dictionary<string, string>));
                }
                else
                {
                    var (descriptor, stream) = await repo.Manifests.FetchAsync(resolveRef, cancellationToken).ConfigureAwait(false);
                    string manifestJson;
                    await using (stream)
                    {
                        using var reader = new StreamReader(stream);
                        manifestJson = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                    }

                    if (output != null)
                    {
                        await File.WriteAllTextAsync(output, manifestJson, cancellationToken).ConfigureAwait(false);
                        AnsiConsole.MarkupLine($"[green]✓[/] Saved manifest to {Markup.Escape(output)}");
                    }
                    else
                    {
                        formatter.WriteJson(manifestJson, pretty);
                    }
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
