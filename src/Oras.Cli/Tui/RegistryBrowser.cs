using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Oras.Services;
using Oras.Credentials;

namespace Oras.Tui;

/// <summary>
/// Interactive registry browser for exploring repositories, tags, and manifests.
/// </summary>
internal class RegistryBrowser
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICredentialService _credentialService;
    private readonly IRegistryService _registryService;
    private readonly DockerConfigStore _configStore;
    private readonly TuiCache _cache;
    private const string MenuSeparator = "───";
    private const string MenuHeader = " ";

    public RegistryBrowser(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _credentialService = serviceProvider.GetRequiredService<ICredentialService>();
        _registryService = serviceProvider.GetRequiredService<IRegistryService>();
        _configStore = serviceProvider.GetRequiredService<DockerConfigStore>();
        _cache = new TuiCache();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // S3-02: Registry connection
        var registryHost = await SelectOrEnterRegistryAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(registryHost))
        {
            return;
        }

        // Check authentication
        var credentials = await _credentialService.GetCredentialsAsync(registryHost, cancellationToken).ConfigureAwait(false);
        if (credentials == null)
        {
            PromptHelper.ShowWarning($"No credentials found for {registryHost}");
            var shouldLogin = PromptHelper.PromptConfirmation("Would you like to login?", true);

            if (shouldLogin)
            {
                credentials = await PromptForCredentialsAsync(registryHost, cancellationToken).ConfigureAwait(false);
                if (credentials == null)
                {
                    return;
                }
            }
        }

        // Verify connection with /v2/ endpoint
        if (!await VerifyRegistryConnectionAsync(registryHost, credentials, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // S3-03: Browse repositories
        await BrowseRepositoriesAsync(registryHost, credentials, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> SelectOrEnterRegistryAsync(CancellationToken cancellationToken)
    {
        var registries = await _configStore.ListRegistriesAsync(cancellationToken).ConfigureAwait(false);

        const string enterNew = "Enter new registry URL...";
        const string back = "Back to main menu";

        var prompt = new SelectionPrompt<string>()
            .Title("[green]Select a registry:[/]")
            .WrapAround()
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
            .UseConverter(x => x == MenuSeparator ? "[dim]───[/]" : x == MenuHeader ? " " : x)
            .AddChoiceGroup(MenuHeader, registries.Concat(new[] { enterNew }).ToArray())
            .AddChoiceGroup(MenuSeparator, back);

        var selection = AnsiConsole.Prompt(prompt);

        if (selection == back)
        {
            return null;
        }

        if (selection == enterNew)
        {
            return PromptHelper.PromptText("Registry host (e.g., localhost:5000, ghcr.io):");
        }

        return selection;
    }

    private async Task<(string Username, string Password)?> PromptForCredentialsAsync(
        string registryHost,
        CancellationToken cancellationToken)
    {
        var username = PromptHelper.PromptText("Username:");
        var password = PromptHelper.PromptSecret("Password:");

        PromptHelper.ShowInfo("Validating credentials...");

        try
        {
            var isValid = await _credentialService.ValidateCredentialsAsync(
                registryHost, username, password, false, false, _registryService, cancellationToken).ConfigureAwait(false);

            if (isValid)
            {
                await _credentialService.StoreCredentialsAsync(registryHost, username, password, cancellationToken).ConfigureAwait(false);
                PromptHelper.ShowSuccess("Login succeeded");
                return (username, password);
            }
            else
            {
                PromptHelper.ShowError("Authentication failed", "Check your username and password");
                return null;
            }
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Login failed: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> VerifyRegistryConnectionAsync(
        string registryHost,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .StartAsync("Connecting to registry...", async ctx =>
            {
                try
                {
                    // TODO: Call /v2/ endpoint to verify connectivity
                    // For now, simulate connection check
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    PromptHelper.ShowSuccess($"Connected to {registryHost}");
                    return true;
                }
                catch (Exception ex)
                {
                    PromptHelper.ShowError($"Failed to connect: {ex.Message}",
                        "Check network connectivity and registry URL.");
                    return false;
                }
            }).ConfigureAwait(false);
    }

    private async Task BrowseRepositoriesAsync(
        string registryHost,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        const string enterRepoOption = "Enter repository name...";
        const string refreshOption = "Refresh repository list";
        const string backOption = "Back to main menu";

        while (true)
        {
            // S3-03: Fetch and display repository list (with caching)
            var repositories = await FetchRepositoriesAsync(registryHost, credentials, false, cancellationToken).ConfigureAwait(false);

            var catalogUnsupported = repositories == null;
            var repoChoices = new List<string>();

            if (catalogUnsupported || repositories!.Count == 0)
            {
                // Registry does not support catalog or returned no repos
                if (catalogUnsupported)
                {
                    PromptHelper.ShowInfo($"This registry does not support repository listing (e.g., ghcr.io).");
                }
                else
                {
                    PromptHelper.ShowInfo("No repositories found in this registry.");
                }
                AnsiConsole.WriteLine();

                repoChoices.Add(enterRepoOption);
                repoChoices.Add(backOption);
            }
            else
            {
                repoChoices.AddRange(repositories);
                repoChoices.Add(enterRepoOption);
                repoChoices.Add(refreshOption);
                repoChoices.Add(backOption);
            }

            var title = catalogUnsupported || repositories == null || repositories.Count == 0
                ? $"[green]Repositories in {Markup.Escape(registryHost)}:[/]"
                : $"[green]Repositories in {Markup.Escape(registryHost)} (Total: {repositories.Count}):[/]";

            var selectedRepo = PromptHelper.PromptSelection(title, repoChoices, enableSearch: true);

            if (selectedRepo == backOption)
            {
                return;
            }

            if (selectedRepo == refreshOption)
            {
                _cache.InvalidatePattern(registryHost);
                PromptHelper.ShowSuccess("Cache cleared. Refreshing...");
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (selectedRepo == enterRepoOption)
            {
                var repoPath = PromptHelper.PromptText(
                    "Repository path (e.g., [green]oras-project/oras[/]):");
                if (string.IsNullOrWhiteSpace(repoPath))
                {
                    continue;
                }
                selectedRepo = repoPath.Trim();
            }

            // Show context menu for repository
            await ShowRepositoryContextMenuAsync(registryHost, selectedRepo, credentials, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Fetches the repository list from the registry catalog API.
    /// Returns null when the catalog API is not supported (e.g., ghcr.io).
    /// Returns an empty list when catalog succeeds but no repositories exist.
    /// </summary>
    private async Task<List<string>?> FetchRepositoriesAsync(
        string registryHost,
        (string Username, string Password)? credentials,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"repos:{registryHost}";

        if (!forceRefresh)
        {
            var cached = _cache.Get<List<string>?>(cacheKey);
            if (cached.Found)
            {
                PromptHelper.ShowCachedIndicator();
                return cached.Value;
            }
        }

        return await AnsiConsole.Status()
            .StartAsync("Fetching repositories...", async ctx =>
            {
                try
                {
                    // TODO: Replace mock data with IRegistry.ListRepositoriesAsync()
                    // When the real API is integrated, a 404/405 or NotSupportedException
                    // from the catalog endpoint should be caught below and return null
                    // to signal "catalog not supported".
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    var repos = new List<string>
                    {
                        "example/app",
                        "example/service",
                        "myorg/artifact",
                        "test/demo"
                    };
                    _cache.Set(cacheKey, repos);
                    return repos;
                }
                catch (NotSupportedException)
                {
                    // Catalog API not supported by this registry — return null
                    return null;
                }
                catch (Exception ex)
                {
                    PromptHelper.ShowError($"Failed to fetch repositories: {Markup.Escape(ex.Message)}");
                    // Treat unexpected errors as catalog unavailable so the user
                    // can still enter a repository name manually.
                    return null;
                }
            }).ConfigureAwait(false);
    }

    private async Task ShowRepositoryContextMenuAsync(
        string registryHost,
        string repository,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        var actions = new[]
        {
            "Browse Tags",
            "Copy entire repository",
            "Backup repository",
            "Back"
        };

        var action = PromptHelper.PromptSelection(
            $"[green]Actions for {Markup.Escape(repository)}:[/]",
            actions);

        switch (action)
        {
            case "Browse Tags":
                await BrowseTagsAsync(registryHost, repository, credentials, cancellationToken).ConfigureAwait(false);
                break;

            case "Copy entire repository":
                await HandleCopyRepositoryAsync(registryHost, repository, cancellationToken).ConfigureAwait(false);
                break;

            case "Backup repository":
                await HandleBackupRepositoryAsync(registryHost, repository, cancellationToken).ConfigureAwait(false);
                break;

            case "Back":
                break;
        }
    }

    private async Task HandleCopyRepositoryAsync(
        string registryHost,
        string repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var destRegistry = PromptHelper.PromptText(
                "Destination registry (e.g., [green]ghcr.io[/]):");
            if (string.IsNullOrWhiteSpace(destRegistry))
            {
                return;
            }

            var destRepo = PromptHelper.PromptText(
                $"Destination repository (default: [green]{repository}[/]):",
                defaultValue: repository);

            var source = $"{registryHost}/{repository}";
            var destination = $"{destRegistry.Trim()}/{destRepo}";

            var escapedSource = Markup.Escape(source);
            var escapedDest = Markup.Escape(destination);

            AnsiConsole.MarkupLine($"\n[bold]Copying repository {escapedSource} => {escapedDest}[/]");
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
                    var listTask = ctx.AddTask("Listing tags");
                    var copyTask = ctx.AddTask("Copying tags (0/5)", maxValue: 5);

                    // Simulate listing tags
                    for (var i = 0; i <= 100; i += 20)
                    {
                        listTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    listTask.Value = 100;

                    // Simulate copying tags
                    for (var tag = 1; tag <= 5; tag++)
                    {
                        copyTask.Description = $"Copying tags ({tag}/5)";
                        copyTask.Increment(1);
                        await Task.Delay(300, cancellationToken).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Copied repository {source} => {destination}");
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

    private async Task HandleBackupRepositoryAsync(
        string registryHost,
        string repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var outputPath = PromptHelper.PromptText(
                "Backup directory:", defaultValue: $"./backup-{repository.Replace("/", "-")}");

            var source = $"{registryHost}/{repository}";
            var escapedSource = Markup.Escape(source);
            var escapedPath = Markup.Escape(outputPath);

            AnsiConsole.MarkupLine($"\n[bold]Backing up {escapedSource}[/]");
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
                    var listTask = ctx.AddTask("Listing tags");
                    var downloadTask = ctx.AddTask("Downloading tags (0/5)", maxValue: 5);
                    var writeTask = ctx.AddTask($"Writing to {escapedPath}");

                    // Simulate listing
                    for (var i = 0; i <= 100; i += 20)
                    {
                        listTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    listTask.Value = 100;

                    // Simulate downloading
                    for (var tag = 1; tag <= 5; tag++)
                    {
                        downloadTask.Description = $"Downloading tags ({tag}/5)";
                        downloadTask.Increment(1);
                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }

                    // Simulate writing
                    for (var i = 0; i <= 100; i += 25)
                    {
                        writeTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    writeTask.Value = 100;
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Backed up {source} to {outputPath}");
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

    /// <summary>
    /// Browse tags for a given registry and repository. Public so Dashboard
    /// can invoke it directly for the "Browse Repository Tags" shortcut.
    /// </summary>
    public async Task BrowseTagsAsync(
        string registryHost,
        string repository,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        const string refreshOption = "Refresh tag list";
        const string backOption = "Back to repository list";

        while (true)
        {
            // S3-04: Fetch and display tags (with caching)
            var tags = await FetchTagsAsync(registryHost, repository, credentials, false, cancellationToken).ConfigureAwait(false);

            if (tags == null || tags.Count == 0)
            {
                PromptHelper.ShowInfo($"No tags found for {repository}.");
                AnsiConsole.WriteLine();
                PromptHelper.PromptText("Press Enter to go back...", allowEmpty: true);
                return;
            }

            var tagChoices = new List<string>(tags);
            tagChoices.Add(refreshOption);
            tagChoices.Add(backOption);

            var selectedTag = PromptHelper.PromptSelection(
                $"[green]Tags for {repository} (Total: {tags.Count}):[/]",
                tagChoices,
                enableSearch: true);

            if (selectedTag == backOption)
            {
                return;
            }

            if (selectedTag == refreshOption)
            {
                _cache.InvalidatePattern($"{registryHost}/{repository}");
                PromptHelper.ShowSuccess("Cache cleared. Refreshing...");
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                continue;
            }

            // Show context menu for tag
            var reference = $"{registryHost}/{repository}:{selectedTag}";
            await ShowTagContextMenuAsync(reference, credentials, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ShowTagContextMenuAsync(
        string reference,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        var prompt = new SelectionPrompt<string>()
            .Title($"[green]Actions for {Markup.Escape(reference)}:[/]")
            .WrapAround()
            .PageSize(10)
            .UseConverter(x => x == MenuSeparator ? "[dim]───[/]" : x == MenuHeader ? " " : x)
            .AddChoiceGroup(MenuHeader, "Inspect Manifest", "Pull to directory", "Copy to...", "Backup to local", "Tag with...", "Delete")
            .AddChoiceGroup(MenuSeparator, "Back");

        var action = AnsiConsole.Prompt(prompt);

        switch (action)
        {
            case "Inspect Manifest":
                var inspector = new ManifestInspector(_serviceProvider);
                await inspector.InspectAsync(reference, credentials, cancellationToken).ConfigureAwait(false);
                break;

            case "Pull to directory":
                await HandlePullTagAsync(reference, cancellationToken).ConfigureAwait(false);
                break;

            case "Copy to...":
                await HandleCopyTagAsync(reference, cancellationToken).ConfigureAwait(false);
                break;

            case "Backup to local":
                await HandleBackupTagAsync(reference, cancellationToken).ConfigureAwait(false);
                break;

            case "Tag with...":
                await HandleTagWithAsync(reference, cancellationToken).ConfigureAwait(false);
                break;

            case "Delete":
                await HandleDeleteTagAsync(reference, cancellationToken).ConfigureAwait(false);
                break;

            case "Back":
                break;
        }
    }

    private async Task<List<string>?> FetchTagsAsync(
        string registryHost,
        string repository,
        (string Username, string Password)? credentials,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"tags:{registryHost}/{repository}";

        if (!forceRefresh)
        {
            var cached = _cache.Get<List<string>?>(cacheKey);
            if (cached.Found)
            {
                PromptHelper.ShowCachedIndicator();
                return cached.Value;
            }
        }

        return await AnsiConsole.Status()
            .StartAsync("Fetching tags...", async ctx =>
            {
                try
                {
                    // TODO: Call IRepository.ListTagsAsync()
                    // For now, return mock data
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    var tagsList = new List<string>
                    {
                        "latest",
                        "v1.0",
                        "v1.1",
                        "v2.0-beta",
                        "develop"
                    };
                    _cache.Set(cacheKey, tagsList);
                    return tagsList;
                }
                catch (Exception ex)
                {
                    PromptHelper.ShowError($"Failed to fetch tags: {ex.Message}");
                    return null;
                }
            }).ConfigureAwait(false);
    }

    private async Task HandlePullTagAsync(string reference, CancellationToken cancellationToken)
    {
        var outputDir = PromptHelper.PromptText(
            "Output directory:", defaultValue: "./");

        await ArtifactActions.RunPullAsync(
            _serviceProvider,
            reference,
            outputDir,
            includeReferrers: false,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleCopyTagAsync(string reference, CancellationToken cancellationToken)
    {
        var destination = PromptHelper.PromptText(
            "Destination reference (e.g., [green]ghcr.io/myorg/backup:v1[/]):");
        if (string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        await ArtifactActions.RunCopyAsync(
            _serviceProvider,
            reference,
            destination.Trim(),
            includeReferrers: false,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleBackupTagAsync(string reference, CancellationToken cancellationToken)
    {
        var outputPath = PromptHelper.PromptText(
            "Backup path:", defaultValue: "./backup");

        await ArtifactActions.RunBackupAsync(
            _serviceProvider,
            reference,
            outputPath,
            includeReferrers: false,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleTagWithAsync(string reference, CancellationToken cancellationToken)
    {
        var tagsInput = PromptHelper.PromptText(
            "New tags (space-separated, e.g., [green]latest stable[/]):");
        if (string.IsNullOrWhiteSpace(tagsInput))
        {
            return;
        }

        var tags = tagsInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        await ArtifactActions.RunTagAsync(
            _serviceProvider,
            reference,
            tags,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleDeleteTagAsync(string reference, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();
        PromptHelper.ShowWarning($"You are about to delete: {reference}");
        AnsiConsole.WriteLine();

        var confirmed = PromptHelper.PromptConfirmation(
            "[red]Are you sure you want to delete this tag?[/]",
            false);

        await ArtifactActions.RunDeleteAsync(
            _serviceProvider,
            reference,
            confirmed,
            cancellationToken).ConfigureAwait(false);
    }
}
