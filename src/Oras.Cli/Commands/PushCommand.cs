using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using OrasProject.Oras.Oci;
using OrasProject.Oras;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Push command implementation.
/// </summary>
internal static class PushCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("push", "Push files to a remote registry");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Target reference (e.g., registry/repository:tag)"
        };
        command.Add(referenceArg);

        // Add file paths argument
        var filesArg = new Argument<string[]>("files")
        {
            Description = "Files to push",
            Arity = ArgumentArity.ZeroOrMore
        };
        command.Add(filesArg);

        // Add options
        var remoteOptions = new RemoteOptions();
        var packerOptions = new PackerOptions();
        remoteOptions.ApplyTo(command);
        packerOptions.ApplyTo(command);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var files = parseResult.GetValue(filesArg) ?? [];
                var artifactType = parseResult.GetValue(packerOptions.ArtifactTypeOption);
                var annotations = ParseAnnotations(parseResult.GetValue(packerOptions.AnnotationOption));
                var concurrency = parseResult.GetValue(packerOptions.ConcurrencyOption);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);

                if (files.Length == 0)
                {
                    throw new OrasUsageException(
                        "No files specified for push",
                        "Provide at least one file to push: oras push <reference> <file1> [file2...]");
                }

                // Validate files exist
                foreach (var file in files)
                {
                    if (!File.Exists(file))
                    {
                        throw new OrasUsageException(
                            $"File not found: {file}",
                            "Ensure all file paths are valid and accessible.");
                    }
                }

                AnsiConsole.MarkupLine($"[blue]Pushing to {reference}...[/]");

                var repo = await registryService.CreateRepositoryAsync(
                    reference,
                    username,
                    password,
                    plainHttp,
                    insecure,
                    cancellationToken).ConfigureAwait(false);

                // Create descriptors for files
                var fileDescriptors = new List<Descriptor>();
                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);

                    // Compute digest before pushing
                    var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
                    var digest = ComputeSha256Digest(fileBytes);

                    var descriptor = new Descriptor
                    {
                        Digest = digest,
                        MediaType = "application/octet-stream",
                        Size = fileInfo.Length,
                        Annotations = new Dictionary<string, string>
                        {
                            [AnnotationTitle] = Path.GetFileName(filePath)
                        }
                    };

                    await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                    // Push blob
                    await repo.Blobs.PushAsync(descriptor, fileStream, cancellationToken).ConfigureAwait(false);

                    fileDescriptors.Add(descriptor);
                    AnsiConsole.MarkupLine($"[green]✓[/] Pushed {Markup.Escape(Path.GetFileName(filePath))}");
                }

                // Pack manifest
                artifactType ??= "application/vnd.unknown.artifact.v1";
                var packOptions = new PackManifestOptions
                {
                    Layers = fileDescriptors
                };

                if (annotations != null && annotations.Count > 0)
                {
                    packOptions.ManifestAnnotations = annotations;
                }

                var manifestDescriptor = await Packer.PackManifestAsync(
                    repo,
                    Packer.ManifestVersion.Version1_1,
                    artifactType,
                    packOptions,
                    cancellationToken).ConfigureAwait(false);

                // Tag if reference has a tag
                var tag = ReferenceHelper.ExtractTag(reference);
                if (!string.IsNullOrEmpty(tag))
                {
                    await repo.TagAsync(manifestDescriptor, tag, cancellationToken).ConfigureAwait(false);
                }

                AnsiConsole.MarkupLine($"[green]✓[/] Pushed {Markup.Escape(reference)}");
                AnsiConsole.MarkupLine($"[dim]Digest: {Markup.Escape(manifestDescriptor.Digest)}[/]");

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

    private const string AnnotationTitle = "org.opencontainers.image.title";

    private static Dictionary<string, string>? ParseAnnotations(string[]? annotations)
    {
        if (annotations == null || annotations.Length == 0)
        {
            return null;
        }

        var result = new Dictionary<string, string>();
        foreach (var annotation in annotations)
        {
            var parts = annotation.Split('=', 2);
            if (parts.Length == 2)
            {
                result[parts[0]] = parts[1];
            }
        }

        return result.Count > 0 ? result : null;
    }

    private static string ComputeSha256Digest(byte[] data)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return $"sha256:{Convert.ToHexStringLower(hash)}";
    }

}
