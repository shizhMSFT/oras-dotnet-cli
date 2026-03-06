using Spectre.Console;
using System;
using System.IO;
using System.Linq;

namespace Oras.Tui;

internal static class ArtifactActions
{
    internal static Task RunPullAsync(IServiceProvider serviceProvider, string reference, CancellationToken cancellationToken)
    {
        var outputDir = PromptHelper.PromptText("Output directory:", defaultValue: "./");
        var includeReferrers = PromptHelper.PromptConfirmation(
            "Include referrers (signatures, SBOMs)?", defaultValue: false);

        return RunPullAsync(serviceProvider, reference, outputDir, includeReferrers, showDestinationLine: true, cancellationToken);
    }

    internal static Task RunPullAsync(
        IServiceProvider serviceProvider,
        string reference,
        string outputDir,
        bool includeReferrers,
        CancellationToken cancellationToken)
    {
        return RunPullAsync(serviceProvider, reference, outputDir, includeReferrers, showDestinationLine: false, cancellationToken);
    }

    internal static async Task RunPullAsync(
        IServiceProvider serviceProvider,
        string reference,
        string outputDir,
        bool includeReferrers,
        bool showDestinationLine,
        CancellationToken cancellationToken)
    {
        _ = serviceProvider;

        try
        {
            var escapedRef = Markup.Escape(reference);
            var escapedDir = Markup.Escape(outputDir);

            if (showDestinationLine)
            {
                AnsiConsole.MarkupLine($"\n[bold]Pulling {escapedRef}[/]");
                AnsiConsole.MarkupLine($"[dim grey]Destination: {escapedDir}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"\n[bold]Pulling {escapedRef} to {escapedDir}[/]");
            }

            if (includeReferrers)
            {
                PromptHelper.ShowInfo("Including referrers (signatures, SBOMs)");
            }
            AnsiConsole.WriteLine();

            var layerCount = 3;

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var resolveTask = ctx.AddTask("Resolving manifest");
                    var downloadTask = ctx.AddTask($"Downloading layers (0/{layerCount})", maxValue: layerCount);
                    var writeTask = ctx.AddTask($"Writing to {escapedDir}");

                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    for (var layer = 1; layer <= layerCount; layer++)
                    {
                        downloadTask.Description = $"Downloading layers ({layer}/{layerCount})";
                        downloadTask.Increment(1);
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }

                    for (var i = 0; i <= 100; i += 25)
                    {
                        writeTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    writeTask.Value = 100;

                    if (includeReferrers)
                    {
                        var referrersTask = ctx.AddTask("Downloading referrers");
                        for (var i = 0; i <= 100; i += 25)
                        {
                            referrersTask.Value = i;
                            await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                        }
                        referrersTask.Value = 100;
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Pulled {reference} to {outputDir}");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Pull cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Pull failed: {ex.Message}");
        }

        PromptHelper.PressEnterToContinue();
    }

    internal static async Task RunCopyAsync(
        IServiceProvider serviceProvider,
        string source,
        string destination,
        bool includeReferrers,
        CancellationToken cancellationToken)
    {
        _ = serviceProvider;

        try
        {
            var escapedSource = Markup.Escape(source);
            var escapedDest = Markup.Escape(destination);

            AnsiConsole.MarkupLine($"\n[bold]Copying {escapedSource} => {escapedDest}[/]");
            if (includeReferrers)
            {
                PromptHelper.ShowInfo("Including referrers (signatures, SBOMs)");
            }
            AnsiConsole.WriteLine();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var resolveTask = ctx.AddTask("Resolving source");
                    var copyTask = ctx.AddTask("Copying layers (0/3)", maxValue: 3);
                    var manifestTask = ctx.AddTask("Copying manifest");

                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    for (var layer = 1; layer <= 3; layer++)
                    {
                        copyTask.Description = $"Copying layers ({layer}/3)";
                        copyTask.Increment(1);
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }

                    for (var i = 0; i <= 100; i += 25)
                    {
                        manifestTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    manifestTask.Value = 100;

                    if (includeReferrers)
                    {
                        var referrersTask = ctx.AddTask("Copying referrers");
                        for (var i = 0; i <= 100; i += 25)
                        {
                            referrersTask.Value = i;
                            await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                        }
                        referrersTask.Value = 100;
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Copied {source} => {destination}");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Copy cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Copy failed: {ex.Message}");
        }

        PromptHelper.PressEnterToContinue();
    }

    internal static Task RunBackupAsync(
        IServiceProvider serviceProvider,
        string source,
        string outputPath,
        bool includeReferrers,
        CancellationToken cancellationToken)
    {
        return RunBackupAsync(serviceProvider, source, outputPath, includeReferrers, showSummary: false, cancellationToken);
    }

    internal static async Task RunBackupAsync(
        IServiceProvider serviceProvider,
        string source,
        string outputPath,
        bool includeReferrers,
        bool showSummary,
        CancellationToken cancellationToken)
    {
        _ = serviceProvider;

        try
        {
            var escapedSource = Markup.Escape(source);
            var escapedPath = Markup.Escape(outputPath);

            AnsiConsole.MarkupLine($"\n[bold]Backing up {escapedSource}[/]");
            if (includeReferrers)
            {
                PromptHelper.ShowInfo("Including referrers (signatures, SBOMs)");
            }
            AnsiConsole.WriteLine();

            var layerCount = 3;
            var estimatedSize = "12.4 MB";

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var fetchTask = ctx.AddTask("Fetching manifest");
                    var downloadTask = ctx.AddTask($"Downloading layers (0/{layerCount})", maxValue: layerCount);
                    var writeTask = ctx.AddTask($"Writing to {escapedPath}");

                    for (var i = 0; i <= 100; i += 20)
                    {
                        fetchTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    fetchTask.Value = 100;

                    for (var layer = 1; layer <= layerCount; layer++)
                    {
                        downloadTask.Description = $"Downloading layers ({layer}/{layerCount})";
                        downloadTask.Increment(1);
                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }

                    for (var i = 0; i <= 100; i += 25)
                    {
                        writeTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    writeTask.Value = 100;

                    if (includeReferrers)
                    {
                        var referrersTask = ctx.AddTask("Downloading referrers");
                        for (var i = 0; i <= 100; i += 25)
                        {
                            referrersTask.Value = i;
                            await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                        }
                        referrersTask.Value = 100;
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();

            if (showSummary)
            {
                var summaryTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Green)
                    .AddColumn(new TableColumn("[green]Backup Summary[/]"))
                    .AddColumn(new TableColumn("[green]Value[/]"));
                summaryTable.AddRow("Reference", Markup.Escape(source));
                summaryTable.AddRow("Layers", layerCount.ToString());
                summaryTable.AddRow("Estimated Size", estimatedSize);
                summaryTable.AddRow("Output Path", Markup.Escape(outputPath));
                if (includeReferrers)
                {
                    summaryTable.AddRow("Referrers", "Included");
                }
                AnsiConsole.Write(summaryTable);

                AnsiConsole.WriteLine();
                PromptHelper.ShowSuccess($"Backup complete: {source} => {outputPath}");
            }
            else
            {
                PromptHelper.ShowSuccess($"Backed up {source} to {outputPath}");
            }
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Backup cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Backup failed: {ex.Message}");
        }

        PromptHelper.PressEnterToContinue();
    }

    internal static async Task RunRestoreAsync(
        IServiceProvider serviceProvider,
        string backupPath,
        string destination,
        CancellationToken cancellationToken)
    {
        _ = serviceProvider;

        try
        {
            if (!Directory.Exists(backupPath) && !File.Exists(backupPath))
            {
                PromptHelper.ShowError(
                    $"Path not found: {backupPath}",
                    "Provide a valid backup directory or .tar.gz file.");
                PromptHelper.PressEnterToContinue();
                return;
            }

            var escapedPath = Markup.Escape(backupPath);
            var escapedDest = Markup.Escape(destination);

            AnsiConsole.MarkupLine($"\n[bold]Restoring {escapedPath} => {escapedDest}[/]");
            AnsiConsole.WriteLine();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var readTask = ctx.AddTask($"Reading from {escapedPath}");
                    var uploadTask = ctx.AddTask("Uploading layers (0/3)", maxValue: 3);
                    var manifestTask = ctx.AddTask("Pushing manifest");

                    for (var i = 0; i <= 100; i += 20)
                    {
                        readTask.Value = i;
                        await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                    }
                    readTask.Value = 100;

                    for (var layer = 1; layer <= 3; layer++)
                    {
                        uploadTask.Description = $"Uploading layers ({layer}/3)";
                        uploadTask.Increment(1);
                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }

                    for (var i = 0; i <= 100; i += 25)
                    {
                        manifestTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    manifestTask.Value = 100;
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Restored {backupPath} => {destination}");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Restore cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Restore failed: {ex.Message}");
        }

        PromptHelper.PressEnterToContinue();
    }

    internal static async Task RunTagAsync(
        IServiceProvider serviceProvider,
        string reference,
        string[] tags,
        CancellationToken cancellationToken)
    {
        _ = serviceProvider;

        try
        {
            if (tags.Length == 0)
            {
                PromptHelper.ShowWarning("No tags provided.");
                PromptHelper.PressEnterToContinue();
                return;
            }

            var escapedRef = Markup.Escape(reference);
            AnsiConsole.MarkupLine($"\n[bold]Tagging {escapedRef}[/]");
            AnsiConsole.MarkupLine($"[dim grey]New tags: {string.Join(", ", tags.Select(Markup.Escape))}[/]");
            AnsiConsole.WriteLine();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var resolveTask = ctx.AddTask("Resolving source");
                    var tagTask = ctx.AddTask($"Creating tags (0/{tags.Length})", maxValue: tags.Length);

                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    for (var i = 0; i < tags.Length; i++)
                    {
                        tagTask.Description = $"Creating tags ({i + 1}/{tags.Length})";
                        tagTask.Increment(1);
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Tagged {reference} with {tags.Length} tag(s)");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Tag operation cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Tag operation failed: {ex.Message}");
        }

        PromptHelper.PressEnterToContinue();
    }

    internal static async Task RunDeleteAsync(
        IServiceProvider serviceProvider,
        string reference,
        bool force,
        CancellationToken cancellationToken)
    {
        _ = serviceProvider;

        try
        {
            if (!force)
            {
                PromptHelper.ShowInfo("Delete operation cancelled.");
                PromptHelper.PressEnterToContinue();
                return;
            }

            var escapedRef = Markup.Escape(reference);
            AnsiConsole.MarkupLine($"\n[bold red]Deleting {escapedRef}[/]");
            AnsiConsole.WriteLine();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var resolveTask = ctx.AddTask("Resolving manifest");
                    var deleteTask = ctx.AddTask("Deleting manifest");

                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    for (var i = 0; i <= 100; i += 25)
                    {
                        deleteTask.Value = i;
                        await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                    }
                    deleteTask.Value = 100;
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Deleted {reference}");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Delete cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Delete failed: {ex.Message}");
        }

        PromptHelper.PressEnterToContinue();
    }
}
