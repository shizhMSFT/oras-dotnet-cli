using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;
using OrasProject.Oras.Oci;

namespace Oras.Commands;

/// <summary>
/// Restore command implementation - restore an OCI artifact from a local
/// backup (OCI layout directory or tar archive) to a registry.
/// </summary>
internal static class RestoreCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("restore", "Restore an OCI artifact from a local backup to a registry");

        // Add path argument (required) — source backup
        var pathArg = new Argument<string>("path")
        {
            Description = "Input path (OCI layout directory or .tar/.tar.gz archive)"
        };
        command.Add(pathArg);

        // Add reference argument (required) — destination registry
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Destination reference (e.g., ghcr.io/myorg/restored:v1.0)"
        };
        command.Add(referenceArg);

        // Add recursive option
        var recursiveOpt = new Option<bool>("--recursive", "-r")
        {
            Description = "Include referrers (signatures, SBOMs)",
            DefaultValueFactory = _ => false
        };
        command.Add(recursiveOpt);

        // Add concurrency option
        var concurrencyOpt = new Option<int>("--concurrency")
        {
            Description = "Number of concurrent transfers",
            DefaultValueFactory = _ => 3
        };
        command.Add(concurrencyOpt);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var path = parseResult.GetValue(pathArg)!;
                var reference = parseResult.GetValue(referenceArg)!;
                var recursive = parseResult.GetValue(recursiveOpt);
                var concurrency = parseResult.GetValue(concurrencyOpt);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // Validate input path exists
                var isArchive = BackupCommand.IsArchivePath(path);
                if (isArchive)
                {
                    throw new OrasException(
                        "Archive restore (.tar/.tar.gz) is not yet supported",
                        "Use an OCI layout directory as the source instead.");
                }

                if (!Directory.Exists(path))
                {
                    throw new OrasUsageException(
                        $"Backup directory not found: {path}",
                        "Provide a valid path to an OCI layout directory.");
                }

                // Validate destination reference
                CopyCommand.ValidateReference(reference, "destination");

                // Validate OCI layout
                var ociLayoutPath = Path.Combine(path, "oci-layout");
                if (!File.Exists(ociLayoutPath))
                {
                    throw new OrasUsageException(
                        $"Not a valid OCI layout: missing oci-layout file in {path}",
                        "Ensure the directory was created by 'oras backup' or another OCI-compliant tool.");
                }

                var indexPath = Path.Combine(path, "index.json");
                if (!File.Exists(indexPath))
                {
                    throw new OrasUsageException(
                        $"Not a valid OCI layout: missing index.json in {path}",
                        "Ensure the directory was created by 'oras backup' or another OCI-compliant tool.");
                }

                // Create destination repository
                AnsiConsole.MarkupLine($"[blue]Restoring[/] to {Markup.Escape(reference)}...");
                var repo = await registryService.CreateRepositoryAsync(
                    reference, username, password, plainHttp, insecure, cancellationToken).ConfigureAwait(false);

                // Parse index.json and push manifest + blobs
                var indexJson = await File.ReadAllTextAsync(indexPath, cancellationToken).ConfigureAwait(false);
                using var indexDoc = JsonDocument.Parse(indexJson);
                var manifests = indexDoc.RootElement.GetProperty("manifests");

                var blobsDir = Path.Combine(path, "blobs", "sha256");

                foreach (var manifestEntry in manifests.EnumerateArray())
                {
                    var manifestDigest = manifestEntry.GetProperty("digest").GetString()!;
                    var manifestMediaType = manifestEntry.GetProperty("mediaType").GetString()!;
                    var manifestSize = manifestEntry.GetProperty("size").GetInt64();

                    // Read manifest blob
                    var manifestHash = manifestDigest.Replace("sha256:", "");
                    var manifestBlobPath = Path.Combine(blobsDir, manifestHash);
                    var manifestBytes = await File.ReadAllBytesAsync(manifestBlobPath, cancellationToken).ConfigureAwait(false);
                    var manifestJson = System.Text.Encoding.UTF8.GetString(manifestBytes);

                    // Parse manifest to find config and layers that need pushing
                    using var manifestDoc = JsonDocument.Parse(manifestJson);
                    var manifestRoot = manifestDoc.RootElement;

                    // Push config blob
                    if (manifestRoot.TryGetProperty("config", out var configEl))
                    {
                        var configDigest = configEl.GetProperty("digest").GetString()!;
                        var configSize = configEl.GetProperty("size").GetInt64();
                        var configMediaType = configEl.GetProperty("mediaType").GetString() ?? "application/octet-stream";
                        var configHash = configDigest.Replace("sha256:", "");
                        var configBlobPath = Path.Combine(blobsDir, configHash);

                        if (File.Exists(configBlobPath))
                        {
                            var configDescriptor = new Descriptor
                            {
                                MediaType = configMediaType,
                                Digest = configDigest,
                                Size = configSize
                            };
                            await using var configStream = File.OpenRead(configBlobPath);
                            await repo.Blobs.PushAsync(configDescriptor, configStream, cancellationToken).ConfigureAwait(false);
                            AnsiConsole.MarkupLine($"[green]✓[/] Pushed config {Markup.Escape(configDigest[..19])}...");
                        }
                    }

                    // Push layer blobs
                    if (manifestRoot.TryGetProperty("layers", out var layersEl))
                    {
                        foreach (var layer in layersEl.EnumerateArray())
                        {
                            var layerDigest = layer.GetProperty("digest").GetString()!;
                            var layerSize = layer.GetProperty("size").GetInt64();
                            var layerMediaType = layer.GetProperty("mediaType").GetString() ?? "application/octet-stream";
                            var layerHash = layerDigest.Replace("sha256:", "");
                            var layerBlobPath = Path.Combine(blobsDir, layerHash);

                            if (File.Exists(layerBlobPath))
                            {
                                var layerDescriptor = new Descriptor
                                {
                                    MediaType = layerMediaType,
                                    Digest = layerDigest,
                                    Size = layerSize
                                };
                                await using var layerStream = File.OpenRead(layerBlobPath);
                                await repo.Blobs.PushAsync(layerDescriptor, layerStream, cancellationToken).ConfigureAwait(false);
                                AnsiConsole.MarkupLine($"[green]✓[/] Pushed layer {Markup.Escape(layerDigest[..19])}...");
                            }
                        }
                    }

                    // Push manifest
                    var mDescriptor = new Descriptor
                    {
                        MediaType = manifestMediaType,
                        Digest = manifestDigest,
                        Size = manifestSize
                    };

                    var dstTag = ReferenceHelper.ExtractTag(reference) ?? "latest";
                    await using var manifestPushStream = File.OpenRead(manifestBlobPath);
                    await repo.Manifests.PushAsync(mDescriptor, manifestPushStream, dstTag, cancellationToken).ConfigureAwait(false);
                    AnsiConsole.MarkupLine($"[green]✓[/] Pushed manifest {Markup.Escape(manifestDigest[..19])}...");
                }

                var summary = new RestoreResult(
                    path,
                    reference,
                    recursive,
                    concurrency,
                    "completed");

                if (format == "json")
                {
                    formatter.WriteObject(summary, OutputJsonContext.Default.RestoreResult);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Restored {Markup.Escape(path)} => {Markup.Escape(reference)}");
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
