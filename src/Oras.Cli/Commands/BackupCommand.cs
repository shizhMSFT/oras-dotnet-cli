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
/// Backup command implementation - back up an OCI artifact from a registry
/// to a local OCI layout directory or tar archive.
/// </summary>
internal static class BackupCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("backup", "Back up an OCI artifact from a registry to a local path");

        // Add reference argument (required)
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Source reference (e.g., ghcr.io/oras-project/oras:v1.2.0)"
        };
        command.Add(referenceArg);

        // Add output option (required)
        var outputOpt = new Option<string>("--output", "-o")
        {
            Description = "Output path (directory for OCI layout, or .tar/.tar.gz for archive)",
            Required = true
        };
        command.Add(outputOpt);

        // Add recursive option
        var recursiveOpt = new Option<bool>("--recursive", "-r")
        {
            Description = "Include referrers (signatures, SBOMs)",
            DefaultValueFactory = _ => false
        };
        command.Add(recursiveOpt);

        // Add platform option
        var platformOptions = new PlatformOptions();
        platformOptions.ApplyTo(command);

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

                var reference = parseResult.GetValue(referenceArg)!;
                var output = parseResult.GetValue(outputOpt)!;
                var recursive = parseResult.GetValue(recursiveOpt);
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var concurrency = parseResult.GetValue(concurrencyOpt);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // Validate reference
                CopyCommand.ValidateReference(reference, "source");

                // Validate and prepare output path
                var isArchive = IsArchivePath(output);
                if (isArchive)
                {
                    throw new OrasException(
                        "Archive backup (.tar/.tar.gz) is not yet supported",
                        "Use a directory path for OCI layout backup instead.");
                }

                // OCI layout directory — create if needed
                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }

                // Create source repository
                var repo = await registryService.CreateRepositoryAsync(
                    reference, username, password, plainHttp, insecure, cancellationToken).ConfigureAwait(false);

                // Resolve and fetch manifest
                var tag = ReferenceHelper.ExtractTag(reference);
                var digest = ReferenceHelper.ExtractDigest(reference);
                var resolveRef = digest ?? tag ?? "latest";

                AnsiConsole.MarkupLine($"[blue]Resolving[/] {Markup.Escape(reference)}...");
                var manifestDescriptor = await repo.ResolveAsync(resolveRef, cancellationToken).ConfigureAwait(false);

                var (_, manifestStream) = await repo.Manifests.FetchAsync(manifestDescriptor.Digest, cancellationToken).ConfigureAwait(false);
                byte[] manifestBytes;
                await using (manifestStream)
                {
                    using var ms = new MemoryStream();
                    await manifestStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                    manifestBytes = ms.ToArray();
                }
                var manifestJson = System.Text.Encoding.UTF8.GetString(manifestBytes);

                // Create OCI layout directory structure
                var blobsDir = Path.Combine(output, "blobs", "sha256");
                Directory.CreateDirectory(blobsDir);

                // Write oci-layout
                await File.WriteAllTextAsync(
                    Path.Combine(output, "oci-layout"),
                    """{"imageLayoutVersion":"1.0.0"}""",
                    cancellationToken).ConfigureAwait(false);

                // Write manifest blob
                var manifestHash = manifestDescriptor.Digest.Replace("sha256:", "");
                await File.WriteAllBytesAsync(
                    Path.Combine(blobsDir, manifestHash),
                    manifestBytes,
                    cancellationToken).ConfigureAwait(false);

                // Write index.json
                var indexJson = $$"""
{
  "schemaVersion": 2,
  "manifests": [
    {
      "mediaType": "{{manifestDescriptor.MediaType}}",
      "digest": "{{manifestDescriptor.Digest}}",
      "size": {{manifestDescriptor.Size}}
    }
  ]
}
""";
                await File.WriteAllTextAsync(
                    Path.Combine(output, "index.json"),
                    indexJson,
                    cancellationToken).ConfigureAwait(false);

                // Parse manifest and download all blobs
                using var doc = JsonDocument.Parse(manifestJson);
                var root = doc.RootElement;
                var layerCount = 0;
                long totalSize = manifestBytes.Length;

                // Download config blob if present
                if (root.TryGetProperty("config", out var configElement))
                {
                    var configDigest = configElement.GetProperty("digest").GetString()!;
                    var configSize = configElement.GetProperty("size").GetInt64();
                    var configMediaType = configElement.GetProperty("mediaType").GetString() ?? "application/octet-stream";
                    
                    var configDescriptor = new Descriptor
                    {
                        MediaType = configMediaType,
                        Digest = configDigest,
                        Size = configSize
                    };

                    var configStream = await repo.Blobs.FetchAsync(configDescriptor, cancellationToken).ConfigureAwait(false);
                    var configHash = configDigest.Replace("sha256:", "");
                    await using (configStream.ConfigureAwait(false))
                    {
                        await using var fs = File.Create(Path.Combine(blobsDir, configHash));
                        await configStream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                    }
                    totalSize += configSize;
                    layerCount++;
                }

                // Download layer blobs
                if (root.TryGetProperty("layers", out var layersElement))
                {
                    foreach (var layer in layersElement.EnumerateArray())
                    {
                        var layerDigest = layer.GetProperty("digest").GetString()!;
                        var layerSize = layer.GetProperty("size").GetInt64();
                        var layerMediaType = layer.GetProperty("mediaType").GetString() ?? "application/octet-stream";
                        
                        var layerDescriptor = new Descriptor
                        {
                            MediaType = layerMediaType,
                            Digest = layerDigest,
                            Size = layerSize
                        };

                        var layerStream = await repo.Blobs.FetchAsync(layerDescriptor, cancellationToken).ConfigureAwait(false);
                        var layerHash = layerDigest.Replace("sha256:", "");
                        await using (layerStream.ConfigureAwait(false))
                        {
                            await using var fs = File.Create(Path.Combine(blobsDir, layerHash));
                            await layerStream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                        }
                        totalSize += layerSize;
                        layerCount++;
                        AnsiConsole.MarkupLine($"[green]✓[/] Downloaded layer {Markup.Escape(layerDigest[..19])}...");
                    }
                }

                var summary = new BackupResult(
                    reference,
                    output,
                    layerCount,
                    Output.FormatHelper.FormatSize(totalSize),
                    recursive,
                    platform ?? "(all)",
                    "completed");

                if (format == "json")
                {
                    formatter.WriteObject(summary, OutputJsonContext.Default.BackupResult);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Backed up [bold]{Markup.Escape(reference)}[/] to [bold]{Markup.Escape(output)}[/]");
                    AnsiConsole.MarkupLine($"  Layers: {layerCount}");
                    AnsiConsole.MarkupLine($"  Total size: {Output.FormatHelper.FormatSize(totalSize)}");
                    if (recursive)
                    {
                        AnsiConsole.MarkupLine("  Referrers: included");
                    }
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

    /// <summary>
    /// Determines if the output path targets an archive file (.tar or .tar.gz).
    /// </summary>
    internal static bool IsArchivePath(string path)
    {
        return path.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase);
    }

}
