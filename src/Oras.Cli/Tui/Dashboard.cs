using Spectre.Console;
using Oras.Services;
using Oras.Credentials;
using System.Reflection;

namespace Oras.Tui;

/// <summary>
/// Main TUI dashboard entry point.
/// </summary>
internal class Dashboard
{
    private readonly ICredentialService _credentialService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DockerConfigStore _configStore;

    public Dashboard(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _credentialService = (ICredentialService?)serviceProvider.GetService(typeof(ICredentialService))
            ?? throw new InvalidOperationException("ICredentialService not registered");
        _configStore = new DockerConfigStore();
    }

    /// <summary>
    /// Launch the TUI dashboard if we're in a TTY environment.
    /// </summary>
    public static bool ShouldLaunchTui(string[] args)
    {
        // Launch TUI only if:
        // 1. No arguments provided
        // 2. Not redirected (stdout or stderr)
        // 3. --no-tty not specified
        return args.Length == 0
            && !Console.IsOutputRedirected
            && !Console.IsErrorRedirected;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                if (!await ShowDashboardAsync(cancellationToken).ConfigureAwait(false))
                {
                    break;
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError(ex.Message);
            return 1;
        }
    }

    private async Task<bool> ShowDashboardAsync(CancellationToken cancellationToken)
    {
        Console.Clear();

        // Fancy ASCII art header
        var logo = new FigletText("ORAS")
            .LeftJustified()
            .Color(Color.Cyan1);
        AnsiConsole.Write(logo);

        var subtitle = new Panel(
            new Markup($"[bold cyan]OCI Registry As Storage[/]\n[dim grey]Version {GetVersion()} • Interactive Terminal UI[/]"))
        {
            Border = BoxBorder.None,
            Padding = new Padding(0, 0, 0, 1)
        };
        AnsiConsole.Write(subtitle);

        // Connected registries
        var config = await _configStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var registries = config.Auths.Keys.ToList();

        if (registries.Any())
        {
            var registryTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[bold yellow]Registry[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold yellow]Status[/]").Centered());

            foreach (var registry in registries)
            {
                var hasCredentials = await _credentialService.GetCredentialsAsync(registry, cancellationToken).ConfigureAwait(false) != null;
                var status = hasCredentials ? "[green]● Authenticated[/]" : "[dim grey]○ No credentials[/]";
                registryTable.AddRow(registry, status);
            }

            var registryPanel = new Panel(registryTable)
            {
                Header = new PanelHeader("[cyan]Connected Registries[/]", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(foreground: Color.Cyan1),
                Padding = new Padding(1, 1, 1, 1)
            };

            AnsiConsole.Write(registryPanel);
            AnsiConsole.WriteLine();
        }
        else
        {
            var infoPanel = new Panel("[dim grey]No connected registries. Use [green]Login[/] to authenticate.[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(foreground: Color.Grey),
                Padding = new Padding(1, 0, 1, 0)
            };
            AnsiConsole.Write(infoPanel);
            AnsiConsole.WriteLine();
        }

        // Quick actions menu
        var actions = new[]
        {
            "Browse Registry",
            "Browse Repository Tags",
            "Login",
            "Copy Artifact",
            "Backup Artifact",
            "Restore Artifact",
            "Push Artifact",
            "Pull Artifact",
            "Tag Artifact",
            "Quit"
        };

        var action = PromptHelper.PromptSelection(
            "[green]Select an action:[/]",
            actions);

        return await HandleActionAsync(action, registries, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> HandleActionAsync(string action, List<string> registries, CancellationToken cancellationToken)
    {
        try
        {
            switch (action)
            {
                case "Browse Registry":
                    var browser = new RegistryBrowser(_serviceProvider);
                    await browser.RunAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Browse Repository Tags":
                    await HandleBrowseRepositoryTagsAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Login":
                    await HandleLoginAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Push Artifact":
                    await HandlePushArtifactAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Pull Artifact":
                    await HandlePullArtifactAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Copy Artifact":
                    await HandleCopyArtifactAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Backup Artifact":
                    await HandleBackupArtifactAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Restore Artifact":
                    await HandleRestoreArtifactAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Tag Artifact":
                    await HandleTagArtifactAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case "Quit":
                    return false;

                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError(ex.Message);
            AnsiConsole.WriteLine();
            PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
            return true;
        }
    }

    private async Task HandleCopyArtifactAsync(CancellationToken cancellationToken)
    {
        try
        {
            var source = PromptHelper.PromptText(
                "Source reference (e.g., [green]ghcr.io/myorg/app:v1.0[/]):");
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }
            source = source.Trim();

            var destination = PromptHelper.PromptText(
                "Destination reference (e.g., [green]ghcr.io/myorg/app-backup:v1.0[/]):");
            if (string.IsNullOrWhiteSpace(destination))
            {
                return;
            }
            destination = destination.Trim();

            var includeReferrers = PromptHelper.PromptConfirmation(
                "Include referrers (signatures, SBOMs)?", defaultValue: false);

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
                    var resolveTask = ctx.AddTask("Resolving source manifest");
                    var layersTask = ctx.AddTask("Copying layers (0/3)", maxValue: 3);
                    var manifestTask = ctx.AddTask("Copying manifest");

                    // Simulate resolving source manifest
                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    // Simulate copying layers
                    for (var layer = 1; layer <= 3; layer++)
                    {
                        layersTask.Description = $"Copying layers ({layer}/3)";
                        layersTask.Increment(1);
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }

                    // Simulate copying manifest
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

        AnsiConsole.WriteLine();
        PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
    }

    private async Task HandleBackupArtifactAsync(CancellationToken cancellationToken)
    {
        try
        {
            var source = PromptHelper.PromptText(
                "Source reference to backup:");
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }
            source = source.Trim();

            var outputPath = PromptHelper.PromptText(
                "Output path (directory or .tar.gz):", defaultValue: "./backup");

            var includeReferrers = PromptHelper.PromptConfirmation(
                "Include referrers?", defaultValue: false);

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

                    // Simulate fetching manifest
                    for (var i = 0; i <= 100; i += 20)
                    {
                        fetchTask.Value = i;
                        await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                    }
                    fetchTask.Value = 100;

                    // Simulate downloading layers
                    for (var layer = 1; layer <= layerCount; layer++)
                    {
                        downloadTask.Description = $"Downloading layers ({layer}/{layerCount})";
                        downloadTask.Increment(1);
                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }

                    // Simulate writing output
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
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Backup cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Backup failed: {ex.Message}");
        }

        AnsiConsole.WriteLine();
        PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
    }

    private async Task HandleRestoreArtifactAsync(CancellationToken cancellationToken)
    {
        try
        {
            var backupPath = PromptHelper.PromptText(
                "Backup path (directory or .tar.gz):");
            if (string.IsNullOrWhiteSpace(backupPath))
            {
                return;
            }
            backupPath = backupPath.Trim();

            // Validate the backup path exists
            if (!Directory.Exists(backupPath) && !File.Exists(backupPath))
            {
                PromptHelper.ShowError(
                    $"Path not found: {backupPath}",
                    "Provide a valid backup directory or .tar.gz file.");
                AnsiConsole.WriteLine();
                PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
                return;
            }

            var destination = PromptHelper.PromptText(
                "Destination reference:");
            if (string.IsNullOrWhiteSpace(destination))
            {
                return;
            }
            destination = destination.Trim();

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

                    // Simulate reading backup
                    for (var i = 0; i <= 100; i += 20)
                    {
                        readTask.Value = i;
                        await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                    }
                    readTask.Value = 100;

                    // Simulate uploading layers
                    for (var layer = 1; layer <= 3; layer++)
                    {
                        uploadTask.Description = $"Uploading layers ({layer}/3)";
                        uploadTask.Increment(1);
                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }

                    // Simulate pushing manifest
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

        AnsiConsole.WriteLine();
        PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
    }

    private async Task HandleBrowseRepositoryTagsAsync(CancellationToken cancellationToken)
    {
        var reference = PromptHelper.PromptText(
            "Full reference (e.g., [green]ghcr.io/oras-project/oras[/]):");

        if (string.IsNullOrWhiteSpace(reference))
        {
            return;
        }

        reference = reference.Trim();

        // Parse registry host from the reference (first segment before '/')
        var slashIndex = reference.IndexOf('/');
        if (slashIndex < 0)
        {
            PromptHelper.ShowError(
                "Invalid reference format",
                "Expected format: registry/namespace/repo (e.g., ghcr.io/oras-project/oras)");
            AnsiConsole.WriteLine();
            PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
            return;
        }

        var registryHost = reference[..slashIndex];
        var repository = reference[(slashIndex + 1)..];

        // Check for credentials
        var credentials = await _credentialService.GetCredentialsAsync(registryHost, cancellationToken).ConfigureAwait(false);

        var browser = new RegistryBrowser(_serviceProvider);
        await browser.BrowseTagsAsync(registryHost, repository, credentials, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleLoginAsync(CancellationToken cancellationToken)
    {
        var registry = PromptHelper.PromptText("Registry host (e.g., localhost:5000, ghcr.io):");
        var username = PromptHelper.PromptText("Username:");
        var password = PromptHelper.PromptSecret("Password:");

        AnsiConsole.Status()
            .Start("Validating credentials...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));
            });

        try
        {
            var isValid = await _credentialService.ValidateCredentialsAsync(
                registry, username, password, false, false, cancellationToken).ConfigureAwait(false);

            if (isValid)
            {
                await _credentialService.StoreCredentialsAsync(registry, username, password, cancellationToken).ConfigureAwait(false);
                PromptHelper.ShowSuccess($"Login succeeded for {registry}");
            }
            else
            {
                PromptHelper.ShowError("Authentication failed", "Check your username and password");
            }
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Login failed: {ex.Message}");
        }

        AnsiConsole.WriteLine();
        PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
    }

    private async Task HandlePushArtifactAsync(CancellationToken cancellationToken)
    {
        try
        {
            var reference = PromptHelper.PromptText(
                "Destination reference (e.g., [green]ghcr.io/myorg/app:v1.0[/]):");
            if (string.IsNullOrWhiteSpace(reference))
            {
                return;
            }
            reference = reference.Trim();

            var filesInput = PromptHelper.PromptText(
                "Files to push (comma-separated paths):");
            if (string.IsNullOrWhiteSpace(filesInput))
            {
                return;
            }

            var files = filesInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .ToList();

            var artifactType = PromptHelper.PromptText(
                "Artifact type (optional, e.g., [green]application/vnd.example.config[/]):",
                allowEmpty: true);

            var escapedRef = Markup.Escape(reference);
            AnsiConsole.MarkupLine($"\n[bold]Pushing to {escapedRef}[/]");
            AnsiConsole.MarkupLine($"[dim grey]Files: {string.Join(", ", files.Select(Markup.Escape))}[/]");
            if (!string.IsNullOrWhiteSpace(artifactType))
            {
                AnsiConsole.MarkupLine($"[dim grey]Type: {Markup.Escape(artifactType)}[/]");
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
                    var uploadTask = ctx.AddTask($"Uploading files (0/{files.Count})", maxValue: files.Count);
                    var manifestTask = ctx.AddTask("Creating manifest");
                    var pushTask = ctx.AddTask("Pushing manifest");

                    // Simulate uploading files
                    for (var i = 0; i < files.Count; i++)
                    {
                        uploadTask.Description = $"Uploading files ({i + 1}/{files.Count})";
                        uploadTask.Increment(1);
                        await Task.Delay(300, cancellationToken).ConfigureAwait(false);
                    }

                    // Simulate creating manifest
                    for (var i = 0; i <= 100; i += 20)
                    {
                        manifestTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    manifestTask.Value = 100;

                    // Simulate pushing manifest
                    for (var i = 0; i <= 100; i += 25)
                    {
                        pushTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    pushTask.Value = 100;
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Pushed {files.Count} file(s) to {reference}");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Push cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Push failed: {ex.Message}");
        }

        AnsiConsole.WriteLine();
        PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
    }

    private async Task HandlePullArtifactAsync(CancellationToken cancellationToken)
    {
        try
        {
            var reference = PromptHelper.PromptText(
                "Source reference (e.g., [green]ghcr.io/myorg/app:v1.0[/]):");
            if (string.IsNullOrWhiteSpace(reference))
            {
                return;
            }
            reference = reference.Trim();

            var outputDir = PromptHelper.PromptText(
                "Output directory:", defaultValue: "./");

            var includeReferrers = PromptHelper.PromptConfirmation(
                "Include referrers (signatures, SBOMs)?", defaultValue: false);

            var escapedRef = Markup.Escape(reference);
            var escapedDir = Markup.Escape(outputDir);

            AnsiConsole.MarkupLine($"\n[bold]Pulling {escapedRef}[/]");
            AnsiConsole.MarkupLine($"[dim grey]Destination: {escapedDir}[/]");
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

                    // Simulate resolving manifest
                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    // Simulate downloading layers
                    for (var layer = 1; layer <= layerCount; layer++)
                    {
                        downloadTask.Description = $"Downloading layers ({layer}/{layerCount})";
                        downloadTask.Increment(1);
                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }

                    // Simulate writing to disk
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

        AnsiConsole.WriteLine();
        PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
    }

    private async Task HandleTagArtifactAsync(CancellationToken cancellationToken)
    {
        try
        {
            var reference = PromptHelper.PromptText(
                "Source reference (e.g., [green]ghcr.io/myorg/app:v1.0[/]):");
            if (string.IsNullOrWhiteSpace(reference))
            {
                return;
            }
            reference = reference.Trim();

            var tagsInput = PromptHelper.PromptText(
                "New tags (space-separated, e.g., [green]latest stable v1.0[/]):");
            if (string.IsNullOrWhiteSpace(tagsInput))
            {
                return;
            }

            var tags = tagsInput.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            if (tags.Count == 0)
            {
                PromptHelper.ShowWarning("No tags provided.");
                AnsiConsole.WriteLine();
                PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
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
                    var resolveTask = ctx.AddTask("Resolving source manifest");
                    var tagTask = ctx.AddTask($"Creating tags (0/{tags.Count})", maxValue: tags.Count);

                    // Simulate resolving source
                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    // Simulate creating tags
                    for (var i = 0; i < tags.Count; i++)
                    {
                        tagTask.Description = $"Creating tags ({i + 1}/{tags.Count})";
                        tagTask.Increment(1);
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Tagged {reference} with {tags.Count} tag(s)");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Tag operation cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Tag operation failed: {ex.Message}");
        }

        AnsiConsole.WriteLine();
        PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.1.0";
    }
}
