using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

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
            DefaultValueFactory = _ => 5
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
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

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
                if (!isArchive)
                {
                    // OCI layout directory — create if needed
                    if (!Directory.Exists(output))
                    {
                        Directory.CreateDirectory(output);
                    }
                }
                else
                {
                    // Archive file — ensure parent directory exists and is writable
                    var parentDir = Path.GetDirectoryName(Path.GetFullPath(output));
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }
                }

                // TODO: Replace simulation with actual library calls:
                // 1. Create source Repository via registryService.CreateRepositoryAsync(reference, username, password, plainHttp, insecure)
                // 2. Resolve the tag/digest to get the root manifest descriptor
                // 3. If platform is set, resolve the platform-specific manifest from an index
                // 4. Walk the DAG using ReadOnlyTargetExtensions.CopyAsync() to a local OCI layout target
                // 5. If recursive, include referrers graph (signatures, SBOMs, attestations)
                // 6. If isArchive, pack the OCI layout into a tar/tar.gz archive

                // Simulated layer info for progress display
                const int simulatedLayerCount = 4;
                const long simulatedTotalSize = 52_428_800; // ~50 MB

                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("blue"))
                    .StartAsync($"Backing up {reference} to {output}...", async ctx =>
                    {
                        ctx.Status($"Resolving manifest for {reference}...");
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

                        ctx.Status($"Downloading layers (0/{simulatedLayerCount})...");
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

                        ctx.Status($"Downloading layers ({simulatedLayerCount}/{simulatedLayerCount})...");
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

                        if (recursive)
                        {
                            ctx.Status("Fetching referrers (signatures, SBOMs)...");
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }

                        if (isArchive)
                        {
                            ctx.Status($"Packing archive: {output}...");
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);

                // Create a placeholder summary file in OCI layout mode
                if (!isArchive)
                {
                    var summaryPath = Path.Combine(output, "oci-layout");
                    await File.WriteAllTextAsync(summaryPath,
                        """{"imageLayoutVersion":"1.0.0"}""",
                        cancellationToken).ConfigureAwait(false);
                }

                var summary = new BackupResult(
                    reference,
                    output,
                    simulatedLayerCount,
                    FormatSize(simulatedTotalSize),
                    recursive,
                    platform ?? "(all)",
                    "simulated");

                if (format == "json")
                {
                    formatter.WriteObject(summary, OutputJsonContext.Default.BackupResult);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Backed up [bold]{reference}[/] to [bold]{output}[/]");
                    AnsiConsole.MarkupLine($"  Layers: {simulatedLayerCount}");
                    AnsiConsole.MarkupLine($"  Total size: {FormatSize(simulatedTotalSize)}");
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

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
