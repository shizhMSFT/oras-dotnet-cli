using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Manifest push command - push a manifest to a registry.
/// </summary>
public static class ManifestPushCommand
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
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                if (!File.Exists(file))
                {
                    throw new OrasUsageException(
                        $"File not found: {file}",
                        "Ensure the file path is correct and the file exists.");
                }

                // TODO: Implement using IManifestStore.PushAsync() or IReferencePushable.PushAsync()
                // Read manifest JSON from file, push to registry
                // Returns descriptor
                // For now, stub with NotImplementedException
                
                throw new NotImplementedException(
                    $"Manifest push operation not yet implemented. Would push {file} to {reference}");

                // Expected output:
                // Text: "Pushed manifest to <reference>\nDigest: <digest>"
                // JSON: descriptor object { digest, mediaType, size }
            });
        });

        return command;
    }
}
