using System.CommandLine;
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

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

                var reference = parseResult.GetValue(referenceArg)!;
                var file = parseResult.GetValue(fileArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
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

                // TODO: Implement using IBlobStore.PushAsync()
                // Returns descriptor with digest and size
                // For now, stub with NotImplementedException

                throw new NotImplementedException(
                    $"Blob push operation not yet implemented. Would push {file} to {reference}");

                // Expected output:
                // Text: "Uploaded <digest> <file>"
                // JSON: descriptor object { digest, size, mediaType }
            }).ConfigureAwait(false);
        });

        return command;
    }
}
