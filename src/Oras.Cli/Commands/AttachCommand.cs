using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;
using OrasProject.Oras.Oci;
using OrasProject.Oras;

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
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var files = parseResult.GetValue(filesArg) ?? Array.Empty<string>();
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var artifactType = parseResult.GetValue(packerOptions.ArtifactTypeOption);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);

                // Validate required artifact type
                if (string.IsNullOrEmpty(artifactType))
                {
                    throw new OrasUsageException(
                        "Option '--artifact-type' is required for attach command",
                        "Specify the artifact type with --artifact-type <type>");
                }

                var formatter = FormatOptions.CreateFormatter(format);

                // Create repository and resolve subject
                var repo = await registryService.CreateRepositoryAsync(
                    reference, username, password, plainHttp, insecure, cancellationToken).ConfigureAwait(false);

                // Resolve subject (parent manifest)
                var resolveRef = ReferenceHelper.ExtractDigest(reference) ?? ReferenceHelper.ExtractTag(reference) ?? "latest";
                var subjectDescriptor = await repo.ResolveAsync(resolveRef, cancellationToken).ConfigureAwait(false);

                // Push file blobs (if any)
                var layerDescriptors = new List<Descriptor>();
                foreach (var fileSpec in files)
                {
                    // Parse file[:mediaType]
                    var parts = fileSpec.Split(':', 2);
                    var filePath = parts[0];
                    var layerMediaType = parts.Length > 1 ? parts[1] : "application/octet-stream";

                    if (!File.Exists(filePath))
                    {
                        throw new OrasUsageException($"File not found: {filePath}", "Ensure file paths are correct.");
                    }

                    var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
                    var digest = ComputeSha256Digest(fileBytes);
                    
                    var descriptor = new Descriptor
                    {
                        MediaType = layerMediaType,
                        Digest = digest,
                        Size = fileBytes.Length,
                        Annotations = new Dictionary<string, string>
                        {
                            ["org.opencontainers.image.title"] = Path.GetFileName(filePath)
                        }
                    };

                    await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    await repo.Blobs.PushAsync(descriptor, fileStream, cancellationToken).ConfigureAwait(false);
                    layerDescriptors.Add(descriptor);
                    AnsiConsole.MarkupLine($"[green]✓[/] Uploaded {Markup.Escape(Path.GetFileName(filePath))}");
                }

                // Pack manifest with subject
                var packOptions = new PackManifestOptions
                {
                    Layers = layerDescriptors,
                    Subject = subjectDescriptor
                };

                var manifestDescriptor = await Packer.PackManifestAsync(
                    repo,
                    Packer.ManifestVersion.Version1_1,
                    artifactType!,
                    packOptions,
                    cancellationToken).ConfigureAwait(false);

                if (format == "json")
                {
                    formatter.WriteDescriptor(new DescriptorResult(
                        manifestDescriptor.MediaType, manifestDescriptor.Digest, 
                        manifestDescriptor.Size, manifestDescriptor.Annotations as Dictionary<string, string>));
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Attached to {Markup.Escape(reference)}");
                    AnsiConsole.MarkupLine($"[dim]Digest: {Markup.Escape(manifestDescriptor.Digest)}[/]");
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
