using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Spectre.Console;
using OrasProject.Oras.Oci;
using System.Text.Json;

namespace Oras.Commands;

/// <summary>
/// Pull command implementation.
/// </summary>
internal static class PullCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("pull", "Pull files from a remote registry");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Source reference (e.g., registry/repository:tag)"
        };
        command.Add(referenceArg);

        // Add options
        var remoteOptions = new RemoteOptions();
        var platformOptions = new PlatformOptions();
        remoteOptions.ApplyTo(command);
        platformOptions.ApplyTo(command);

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output directory for pulled files",
            DefaultValueFactory = _ => "."
        };
        command.Add(outputOption);

        var keepOldFilesOption = new Option<bool>("--keep-old-files")
        {
            Description = "Do not overwrite existing files"
        };
        command.Add(keepOldFilesOption);

        var concurrencyOption = new Option<int>("--concurrency")
        {
            Description = "Maximum number of concurrent operations",
            DefaultValueFactory = _ => 3
        };
        command.Add(concurrencyOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var outputDir = parseResult.GetValue(outputOption)!;
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var keepOldFiles = parseResult.GetValue(keepOldFilesOption);
                var concurrency = parseResult.GetValue(concurrencyOption);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);

                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                AnsiConsole.MarkupLine($"[blue]Pulling from {reference}...[/]");

                var repo = await registryService.CreateRepositoryAsync(
                    reference,
                    username,
                    password,
                    plainHttp,
                    insecure,
                    cancellationToken).ConfigureAwait(false);

                // Resolve the tag or digest
                var tag = ReferenceHelper.ExtractTag(reference);
                var digest = ReferenceHelper.ExtractDigest(reference);

                Descriptor manifestDescriptor;
                if (!string.IsNullOrEmpty(digest))
                {
                    manifestDescriptor = new Descriptor { Digest = digest, MediaType = "application/vnd.oci.image.manifest.v1+json", Size = 0 };
                }
                else if (!string.IsNullOrEmpty(tag))
                {
                    manifestDescriptor = await repo.ResolveAsync(tag, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new OrasUsageException(
                        "No tag or digest specified in reference",
                        "Specify a tag (e.g., :latest) or digest (e.g., @sha256:...)");
                }

                AnsiConsole.MarkupLine($"[dim]Manifest digest: {Markup.Escape(manifestDescriptor.Digest)}[/]");

                // Fetch the manifest
                var (fetchedDescriptor, manifestStream) = await repo.Manifests.FetchAsync(
                    manifestDescriptor.Digest, cancellationToken).ConfigureAwait(false);
                string manifestJson;
                await using (manifestStream)
                {
                    using var reader = new StreamReader(manifestStream);
                    manifestJson = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                }

                // Parse layers from manifest JSON
                using var doc = JsonDocument.Parse(manifestJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("layers", out var layersElement))
                {
                    AnsiConsole.MarkupLine("[yellow]No layers found in manifest[/]");
                    return 0;
                }

                var pulledFiles = new List<string>();

                foreach (var layer in layersElement.EnumerateArray())
                {
                    var layerDigest = layer.GetProperty("digest").GetString()!;
                    var layerMediaType = layer.GetProperty("mediaType").GetString() ?? "application/octet-stream";
                    var layerSize = layer.GetProperty("size").GetInt64();
                    
                    // Get filename from annotations
                    string fileName;
                    if (layer.TryGetProperty("annotations", out var annotationsEl) &&
                        annotationsEl.TryGetProperty("org.opencontainers.image.title", out var titleEl))
                    {
                        fileName = titleEl.GetString()!;
                    }
                    else
                    {
                        // Use digest as filename if no title annotation
                        fileName = layerDigest.Replace("sha256:", "").Substring(0, 12);
                    }

                    var filePath = Path.Combine(outputDir, fileName);
                    
                    if (keepOldFiles && File.Exists(filePath))
                    {
                        AnsiConsole.MarkupLine($"[yellow]Skipping {Markup.Escape(fileName)} (already exists)[/]");
                        continue;
                    }

                    var layerDescriptor = new Descriptor
                    {
                        MediaType = layerMediaType,
                        Digest = layerDigest,
                        Size = layerSize
                    };

                    var blobStream = await repo.Blobs.FetchAsync(layerDescriptor, cancellationToken).ConfigureAwait(false);
                    await using (blobStream)
                    {
                        // Ensure parent directory exists
                        var parentDir = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                        {
                            Directory.CreateDirectory(parentDir);
                        }
                        
                        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                        await blobStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    }

                    pulledFiles.Add(fileName);
                    AnsiConsole.MarkupLine($"[green]✓[/] Pulled {Markup.Escape(fileName)}");
                }

                AnsiConsole.MarkupLine($"[green]✓[/] Pulled {Markup.Escape(reference)}");
                AnsiConsole.MarkupLine($"[dim]Files: {pulledFiles.Count}[/]");

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

}
