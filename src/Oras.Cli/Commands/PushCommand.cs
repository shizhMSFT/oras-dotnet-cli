using System.CommandLine;
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

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

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
                    insecure).ConfigureAwait(false);

                // Create descriptors for files
                var fileDescriptors = new List<Descriptor>();
                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                    var descriptor = new Descriptor
                    {
                        Digest = string.Empty, // Will be set by library
                        MediaType = "application/octet-stream",
                        Size = fileInfo.Length,
                        Annotations = new Dictionary<string, string>
                        {
                            [AnnotationTitle] = Path.GetFileName(filePath)
                        }
                    };

                    // Push blob
                    await repo.Blobs.PushAsync(descriptor, fileStream).ConfigureAwait(false);
                    fileStream.Close();

                    fileDescriptors.Add(descriptor);
                    AnsiConsole.MarkupLine($"[green]✓[/] Pushed {Path.GetFileName(filePath)}");
                }

                // Pack manifest
                // TODO: Implement with actual Packer API from OrasProject.Oras v0.5.0
                artifactType ??= "application/vnd.oras.artifact.v1";
                // var manifestDescriptor = await Packer.PackManifestAsync(...)

                throw new NotImplementedException(
                    "Packer.PackManifestAsync needs OrasProject.Oras v0.5.0 API integration. " +
                    "The actual API signature differs from expected.");

                // Get tag from reference
                // var tag = ExtractTag(reference);
                // if (!string.IsNullOrEmpty(tag))
                // {
                //     await repo.TagAsync(manifestDescriptor, tag);
                // }

                // AnsiConsole.MarkupLine($"[green]✓[/] Pushed {reference}");
                // AnsiConsole.MarkupLine($"[dim]Digest: {manifestDescriptor.Digest}[/]");

                // return 0;
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

    private static string? ExtractTag(string reference)
    {
        var colonIndex = reference.LastIndexOf(':');
        var slashIndex = reference.LastIndexOf('/');

        if (colonIndex > slashIndex && colonIndex >= 0)
        {
            return reference[(colonIndex + 1)..];
        }

        return null;
    }
}
