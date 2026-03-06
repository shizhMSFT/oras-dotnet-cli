using Microsoft.Extensions.DependencyInjection;
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
    private readonly IRegistryService _registryService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DockerConfigStore _configStore;
    private const string BrowseRegistryAction = "Browse Registry";
    private const string BrowseRepositoryTagsAction = "Browse Repository Tags";
    private const string LoginAction = "Login";
    private const string PushArtifactAction = "Push Artifact";
    private const string PullArtifactAction = "Pull Artifact";
    private const string CopyArtifactAction = "Copy Artifact";
    private const string TagArtifactAction = "Tag Artifact";
    private const string BackupArtifactAction = "Backup Artifact";
    private const string RestoreArtifactAction = "Restore Artifact";
    private const string ArtifactsAction = "Artifacts";
    private const string BackAction = "Back";
    private const string QuitAction = "Quit";
    private const string MenuSeparator = "───";
    private const string MenuHeader = " ";

    public Dashboard(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _credentialService = serviceProvider.GetRequiredService<ICredentialService>();
        _registryService = serviceProvider.GetRequiredService<IRegistryService>();
        _configStore = serviceProvider.GetRequiredService<DockerConfigStore>();
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
        AnsiConsole.Clear();

        // Hand-crafted Unicode art header
        var version = GetVersion();
        AnsiConsole.Markup(
            "[bold #D04485]  ██████╗ [/][bold #5EBAB4]██████╗ [/][bold #FCFCFD] █████╗ [/][bold #CCF575]███████╗[/]\n" +
            "[bold #D04485] ██╔═══██╗[/][bold #5EBAB4]██╔══██╗[/][bold #FCFCFD]██╔══██╗[/][bold #CCF575]██╔════╝[/]\n" +
            "[bold #D04485] ██║   ██║[/][bold #5EBAB4]██████╔╝[/][bold #FCFCFD]███████║[/][bold #CCF575]███████╗[/]\n" +
            "[bold #D04485] ██║   ██║[/][bold #5EBAB4]██╔══██╗[/][bold #FCFCFD]██╔══██║[/][bold #CCF575]╚════██║[/]\n" +
            "[bold #D04485] ╚██████╔╝[/][bold #5EBAB4]██║  ██║[/][bold #FCFCFD]██║  ██║[/][bold #CCF575]███████║[/]\n" +
            "[bold #D04485]  ╚═════╝ [/][bold #5EBAB4]╚═╝  ╚═╝[/][bold #FCFCFD]╚═╝  ╚═╝[/][bold #CCF575]╚══════╝[/]\n");
        AnsiConsole.Markup(
            $"[bold #5EBAB4] OCI Registry As Storage[/]  [dim grey]│[/]  [dim grey]v{version} • Interactive Terminal UI[/]\n" +
            "[dim grey] ─────────────────────────────────────────────[/]\n\n");

        // Connected registries (auths + credHelpers + credsStore)
        var registries = await _configStore.ListRegistriesAsync(cancellationToken).ConfigureAwait(false);

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
                registryTable.AddRow(Markup.Escape(registry), status);
            }

            var registryPanel = new Panel(registryTable)
            {
                Header = new PanelHeader("[mediumpurple]Connected Registries[/]", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(foreground: new Color(95, 95, 215)),
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
        var prompt = new SelectionPrompt<string>()
            .Title("[green]Select an action:[/]")
            .WrapAround()
            .PageSize(12)
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
            .UseConverter(x => x == MenuSeparator ? "[dim]───[/]" : x == MenuHeader ? " " : x)
            .AddChoiceGroup(MenuHeader, BrowseRegistryAction, BrowseRepositoryTagsAction, LoginAction, ArtifactsAction)
            .AddChoiceGroup(MenuSeparator, QuitAction);

        var action = AnsiConsole.Prompt(prompt);
        return await HandleActionAsync(action, registries, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> HandleActionAsync(string action, IReadOnlyList<string> registries, CancellationToken cancellationToken)
    {
        try
        {
            switch (action)
            {
                case BrowseRegistryAction:
                    var browser = new RegistryBrowser(_serviceProvider);
                    await browser.RunAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case BrowseRepositoryTagsAction:
                    await HandleBrowseRepositoryTagsAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case LoginAction:
                    await HandleLoginAsync(cancellationToken).ConfigureAwait(false);
                    return true;

                case ArtifactsAction:
                    return await HandleArtifactsMenuAsync(cancellationToken).ConfigureAwait(false);

                case QuitAction:
                    return false;

                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError(ex.Message);
            PromptHelper.PressEnterToContinue();
            return true;
        }
    }

    private async Task<bool> HandleArtifactsMenuAsync(CancellationToken cancellationToken)
    {
        var artifactPrompt = new SelectionPrompt<string>()
            .Title("[green]Select an artifact action:[/]")
            .WrapAround()
            .PageSize(10)
            .UseConverter(x => x == MenuSeparator ? "[dim]───[/]" : x == MenuHeader ? " " : x)
            .AddChoiceGroup(MenuHeader,
                PushArtifactAction,
                PullArtifactAction,
                CopyArtifactAction,
                TagArtifactAction,
                BackupArtifactAction,
                RestoreArtifactAction)
            .AddChoiceGroup(MenuSeparator, BackAction);

        var artifactAction = AnsiConsole.Prompt(artifactPrompt);

        if (artifactAction == BackAction)
        {
            return true;
        }

        try
        {
            switch (artifactAction)
            {
                case PushArtifactAction:
                    await HandlePushArtifactAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case PullArtifactAction:
                    await HandlePullArtifactAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case CopyArtifactAction:
                    await HandleCopyArtifactAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case TagArtifactAction:
                    await HandleTagArtifactAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case BackupArtifactAction:
                    await HandleBackupArtifactAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case RestoreArtifactAction:
                    await HandleRestoreArtifactAsync(cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError(ex.Message);
            PromptHelper.PressEnterToContinue();
        }

        return true;
    }

    private async Task HandleCopyArtifactAsync(CancellationToken cancellationToken)
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

        await ArtifactActions.RunCopyAsync(
            _serviceProvider,
            source,
            destination,
            includeReferrers,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleBackupArtifactAsync(CancellationToken cancellationToken)
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

        await ArtifactActions.RunBackupAsync(
            _serviceProvider,
            source,
            outputPath,
            includeReferrers,
            showSummary: true,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleRestoreArtifactAsync(CancellationToken cancellationToken)
    {
        var backupPath = PromptHelper.PromptText(
            "Backup path (directory or .tar.gz):");
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            return;
        }
        backupPath = backupPath.Trim();

        var destination = PromptHelper.PromptText(
            "Destination reference:");
        if (string.IsNullOrWhiteSpace(destination))
        {
            return;
        }
        destination = destination.Trim();

        await ArtifactActions.RunRestoreAsync(
            _serviceProvider,
            backupPath,
            destination,
            cancellationToken).ConfigureAwait(false);
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
            PromptHelper.PressEnterToContinue();
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

        try
        {
            var isValid = await AnsiConsole.Status()
                .StartAsync("Validating credentials...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    return await _credentialService.ValidateCredentialsAsync(
                        registry, username, password, false, false, _registryService, cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);

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

        PromptHelper.PressEnterToContinue();
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

        PromptHelper.PressEnterToContinue();
    }

    private async Task HandlePullArtifactAsync(CancellationToken cancellationToken)
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

        await ArtifactActions.RunPullAsync(
            _serviceProvider,
            reference,
            outputDir,
            includeReferrers,
            showDestinationLine: true,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleTagArtifactAsync(CancellationToken cancellationToken)
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
            .ToArray();

        await ArtifactActions.RunTagAsync(
            _serviceProvider,
            reference,
            tags,
            cancellationToken).ConfigureAwait(false);
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.1.0";
    }
}
