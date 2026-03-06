using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Manifest push command - push a manifest to a registry.
/// </summary>
internal static class ManifestPushCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("push", "Push a manifest to a registry");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Target reference (registry/repository:tag)"
        };
        command.Add(referenceArg);

        // Add file argument
        var fileArg = new Argument<string>("file")
        {
            Description = "Manifest JSON file to push"
        };
        command.Add(fileArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        // Add media type option
        var mediaTypeOpt = new Option<string?>("--media-type")
        {
            Description = "Media type of the manifest",
            DefaultValueFactory = _ => "application/vnd.oci.image.manifest.v1+json"
        };
        command.Add(mediaTypeOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var file = parseResult.GetValue(fileArg)!;
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var mediaType = parseResult.GetValue(mediaTypeOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                if (!File.Exists(file))
                {
                    throw new OrasUsageException(
                        $"File not found: {file}",
                        "Ensure the file path is correct and the file exists.");
                }

                var repo = await registryService.CreateRepositoryAsync(
                    reference,
                    username,
                    password,
                    plainHttp,
                    insecure,
                    cancellationToken).ConfigureAwait(false);

                var fileBytes = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
                var digest = ComputeSha256Digest(fileBytes);

                var descriptor = new OrasProject.Oras.Oci.Descriptor
                {
                    MediaType = mediaType ?? "application/vnd.oci.image.manifest.v1+json",
                    Digest = digest,
                    Size = fileBytes.Length
                };

                var tag = ReferenceHelper.ExtractTag(reference);

                await using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                if (!string.IsNullOrEmpty(tag))
                {
                    await repo.Manifests.PushAsync(descriptor, fileStream, tag, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await repo.Manifests.PushAsync(descriptor, fileStream, cancellationToken).ConfigureAwait(false);
                }

                if (format == "text")
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Pushed manifest to {Markup.Escape(reference)}");
                    AnsiConsole.MarkupLine($"[dim]Digest: {Markup.Escape(descriptor.Digest)}[/]");
                }
                else
                {
                    formatter.WriteDescriptor(new DescriptorResult(
                        descriptor.MediaType,
                        descriptor.Digest,
                        descriptor.Size,
                        null));
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

    private static string ComputeSha256Digest(byte[] data)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return $"sha256:{Convert.ToHexStringLower(hash)}";
    }
}
