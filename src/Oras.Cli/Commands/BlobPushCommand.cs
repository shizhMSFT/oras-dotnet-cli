using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Blob push command - push a blob to a registry.
/// </summary>
internal static class BlobPushCommand
{
    private static string ComputeSha256Digest(byte[] data)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return $"sha256:{Convert.ToHexStringLower(hash)}";
    }

    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("push", "Push a blob to a registry");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Target reference (registry/repository)"
        };
        command.Add(referenceArg);

        // Add file argument
        var fileArg = new Argument<string>("file")
        {
            Description = "File to push as a blob"
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
            Description = "Media type of the blob",
            DefaultValueFactory = _ => "application/octet-stream"
        };
        command.Add(mediaTypeOpt);

        // Add size option (for verification)
        var sizeOpt = new Option<long?>("--size")
        {
            Description = "Expected size of the blob (for verification)",
            DefaultValueFactory = _ => null
        };
        command.Add(sizeOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var file = parseResult.GetValue(fileArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var mediaType = parseResult.GetValue(mediaTypeOpt);
                var size = parseResult.GetValue(sizeOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                if (!File.Exists(file))
                {
                    throw new OrasUsageException(
                        $"File not found: {file}",
                        "Ensure the file path is correct and the file exists.");
                }

                // Create repository
                var repo = await registryService.CreateRepositoryAsync(
                    reference, username, password, plainHttp, insecure, cancellationToken).ConfigureAwait(false);

                // Read file and compute digest
                var fileInfo = new FileInfo(file);

                // Validate size if provided
                if (size.HasValue && size.Value != fileInfo.Length)
                {
                    throw new OrasUsageException(
                        $"File size mismatch: expected {size.Value} bytes, got {fileInfo.Length} bytes",
                        "Ensure the file has not been modified since the size was determined.");
                }

                var fileBytes = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
                var digest = ComputeSha256Digest(fileBytes);

                var descriptor = new OrasProject.Oras.Oci.Descriptor
                {
                    MediaType = mediaType ?? "application/octet-stream",
                    Digest = digest,
                    Size = fileInfo.Length
                };

                // Push blob
                await using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                await repo.Blobs.PushAsync(descriptor, fileStream, cancellationToken).ConfigureAwait(false);

                // Output result
                if (format == "text")
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Uploaded {Markup.Escape(descriptor.Digest)}");
                    Console.WriteLine(descriptor.Digest);
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
}
