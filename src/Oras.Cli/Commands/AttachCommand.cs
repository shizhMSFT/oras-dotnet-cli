using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Attach command implementation - attach files as a referrer artifact.
/// </summary>
internal static class AttachCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("attach", "Attach files as a referrer artifact to an existing manifest");

        // Add reference argument (parent manifest)
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Parent manifest reference (registry/repository:tag or @digest)"
        };
        command.Add(referenceArg);

        // Add files argument (optional - can attach without files)
        var filesArg = new Argument<string[]>("files")
        {
            Description = "Files to attach with optional media types (file[:type])",
            Arity = ArgumentArity.ZeroOrMore
        };
        command.Add(filesArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add packer options
        var packerOptions = new PackerOptions();
        packerOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var pushService = serviceProvider.GetRequiredService<IPushService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var files = parseResult.GetValue(filesArg) ?? Array.Empty<string>();
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var artifactType = parseResult.GetValue(packerOptions.ArtifactTypeOption);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                // Validate required artifact type
                if (string.IsNullOrEmpty(artifactType))
                {
                    throw new OrasUsageException(
                        "Option '--artifact-type' is required for attach command",
                        "Specify the artifact type with --artifact-type <type>");
                }

                var formatter = FormatOptions.CreateFormatter(format);

                // TODO: Implement using Packer.PackManifestAsync() with PackManifestOptions.Subject
                // 1. Resolve parent manifest to get its descriptor
                // 2. Create manifest with subject field pointing to parent
                // 3. Push blobs and manifest
                // For now, stub with NotImplementedException

                throw new NotImplementedException(
                    $"Attach operation not yet implemented. Would attach {files.Length} files to {reference} " +
                    $"with artifact type {artifactType}");

                // Expected output:
                // Text: referrer manifest descriptor
                // JSON: descriptor object with subject information
            }).ConfigureAwait(false);
        });

        return command;
    }
}
