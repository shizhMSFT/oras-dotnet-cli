using Spectre.Console;
using Oras.Services;
using Oras.Credentials;

namespace Oras.Tui;

/// <summary>
/// Interactive registry browser for exploring repositories, tags, and manifests.
/// </summary>
public class RegistryBrowser
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICredentialService _credentialService;
    private readonly DockerConfigStore _configStore;

    public RegistryBrowser(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _credentialService = (ICredentialService?)serviceProvider.GetService(typeof(ICredentialService))
            ?? throw new InvalidOperationException("ICredentialService not registered");
        _configStore = new DockerConfigStore();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // S3-02: Registry connection
        var registryHost = await SelectOrEnterRegistryAsync(cancellationToken);
        if (string.IsNullOrEmpty(registryHost))
        {
            return;
        }

        // Check authentication
        var credentials = await _credentialService.GetCredentialsAsync(registryHost, cancellationToken);
        if (credentials == null)
        {
            PromptHelper.ShowWarning($"No credentials found for {registryHost}");
            var shouldLogin = PromptHelper.PromptConfirmation("Would you like to login?", true);
            
            if (shouldLogin)
            {
                credentials = await PromptForCredentialsAsync(registryHost, cancellationToken);
                if (credentials == null)
                {
                    return;
                }
            }
        }

        // Verify connection with /v2/ endpoint
        if (!await VerifyRegistryConnectionAsync(registryHost, credentials, cancellationToken))
        {
            return;
        }

        // S3-03: Browse repositories
        await BrowseRepositoriesAsync(registryHost, credentials, cancellationToken);
    }

    private async Task<string?> SelectOrEnterRegistryAsync(CancellationToken cancellationToken)
    {
        var config = await _configStore.LoadAsync(cancellationToken);
        var registries = config.Auths.Keys.ToList();

        var choices = new List<string>(registries);
        choices.Add("Enter new registry URL...");
        choices.Add("Back to main menu");

        var selection = PromptHelper.PromptSelection(
            "[green]Select a registry:[/]",
            choices);

        if (selection == "Back to main menu")
        {
            return null;
        }

        if (selection == "Enter new registry URL...")
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
                registryHost, username, password, false, false, cancellationToken);

            if (isValid)
            {
                await _credentialService.StoreCredentialsAsync(registryHost, username, password, cancellationToken);
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
                    await Task.Delay(500, cancellationToken);
                    PromptHelper.ShowSuccess($"Connected to {registryHost}");
                    return true;
                }
                catch (Exception ex)
                {
                    PromptHelper.ShowError($"Failed to connect: {ex.Message}", 
                        "Check network connectivity and registry URL.");
                    return false;
                }
            });
    }

    private async Task BrowseRepositoriesAsync(
        string registryHost,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            // S3-03: Fetch and display repository list
            var repositories = await FetchRepositoriesAsync(registryHost, credentials, cancellationToken);
            
            if (repositories == null || repositories.Count == 0)
            {
                PromptHelper.ShowInfo("No repositories found in this registry.");
                AnsiConsole.WriteLine();
                PromptHelper.PromptText("Press Enter to go back...", allowEmpty: true);
                return;
            }

            var repoChoices = new List<string>(repositories);
            repoChoices.Add("Back to main menu");

            var selectedRepo = PromptHelper.PromptSelectionWithSearch(
                $"[green]Repositories in {registryHost} (Total: {repositories.Count}):[/]",
                repoChoices);

            if (selectedRepo == "Back to main menu")
            {
                return;
            }

            // S3-04: Browse tags for selected repository
            await BrowseTagsAsync(registryHost, selectedRepo, credentials, cancellationToken);
        }
    }

    private async Task<List<string>?> FetchRepositoriesAsync(
        string registryHost,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .StartAsync("Fetching repositories...", async ctx =>
            {
                try
                {
                    // TODO: Call IRegistry.ListRepositoriesAsync()
                    // For now, return mock data
                    await Task.Delay(500, cancellationToken);
                    return new List<string>
                    {
                        "example/app",
                        "example/service",
                        "myorg/artifact",
                        "test/demo"
                    };
                }
                catch (Exception ex)
                {
                    PromptHelper.ShowError($"Failed to fetch repositories: {ex.Message}");
                    return null;
                }
            });
    }

    private async Task BrowseTagsAsync(
        string registryHost,
        string repository,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            // S3-04: Fetch and display tags
            var tags = await FetchTagsAsync(registryHost, repository, credentials, cancellationToken);
            
            if (tags == null || tags.Count == 0)
            {
                PromptHelper.ShowInfo($"No tags found for {repository}.");
                AnsiConsole.WriteLine();
                PromptHelper.PromptText("Press Enter to go back...", allowEmpty: true);
                return;
            }

            var tagChoices = new List<string>(tags);
            tagChoices.Add("Back to repository list");

            var selectedTag = PromptHelper.PromptSelectionWithSearch(
                $"[green]Tags for {repository} (Total: {tags.Count}):[/]",
                tagChoices);

            if (selectedTag == "Back to repository list")
            {
                return;
            }

            // S3-05: Show manifest inspector
            var reference = $"{registryHost}/{repository}:{selectedTag}";
            var inspector = new ManifestInspector(_serviceProvider);
            await inspector.InspectAsync(reference, credentials, cancellationToken);
        }
    }

    private async Task<List<string>?> FetchTagsAsync(
        string registryHost,
        string repository,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .StartAsync("Fetching tags...", async ctx =>
            {
                try
                {
                    // TODO: Call IRepository.ListTagsAsync()
                    // For now, return mock data
                    await Task.Delay(500, cancellationToken);
                    return new List<string>
                    {
                        "latest",
                        "v1.0",
                        "v1.1",
                        "v2.0-beta",
                        "develop"
                    };
                }
                catch (Exception ex)
                {
                    PromptHelper.ShowError($"Failed to fetch tags: {ex.Message}");
                    return null;
                }
            });
    }
}
