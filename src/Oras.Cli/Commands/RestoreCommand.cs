using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

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
                    if (!File.Exists(path))
                    {
                        throw new OrasUsageException(
                            $"Backup archive not found: {path}",
                            "Provide a valid path to a .tar or .tar.gz backup archive.");
                    }
                }
                else
                {
                    if (!Directory.Exists(path))
                    {
                        throw new OrasUsageException(
                            $"Backup directory not found: {path}",
                            "Provide a valid path to an OCI layout directory.");
                    }
                }

                // Validate destination reference
                CopyCommand.ValidateReference(reference, "destination");

                // TODO: Replace simulation with actual library calls:
                // 1. If isArchive, extract the tar/tar.gz to a temp OCI layout directory
                // 2. Open the OCI layout as a local ITarget (OCI layout store)
                // 3. Create destination Repository via registryService.CreateRepositoryAsync(reference, username, password, plainHttp, insecure)
                // 4. Walk the DAG using ReadOnlyTargetExtensions.CopyAsync() from local store to remote repo
                // 5. If recursive, include referrers graph
                // 6. Tag the manifest at the destination with the reference tag

                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("blue"))
                    .StartAsync($"Restoring {path} to {reference}...", async ctx =>
                    {
                        if (isArchive)
                        {
                            ctx.Status($"Extracting archive: {path}...");
                            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                        }

                        ctx.Status("Reading OCI layout...");
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

                        ctx.Status($"Pushing layers to {reference}...");
                        await Task.Delay(300, cancellationToken).ConfigureAwait(false);

                        if (recursive)
                        {
                            ctx.Status("Pushing referrers (signatures, SBOMs)...");
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }

                        ctx.Status("Verifying...");
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                var summary = new RestoreResult(
                    path,
                    reference,
                    recursive,
                    concurrency,
                    "simulated");

                if (format == "json")
                {
                    formatter.WriteObject(summary, OutputJsonContext.Default.RestoreResult);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]Restored[/] {path} => {reference}");
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
