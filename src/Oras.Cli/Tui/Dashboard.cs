using Spectre.Console;
using Oras.Services;
using Oras.Credentials;
using System.Reflection;

namespace Oras.Tui;

/// <summary>
/// Main TUI dashboard entry point.
/// </summary>
public class Dashboard
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
                if (!await ShowDashboardAsync(cancellationToken))
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
        
        // Header
        var headerPanel = new Panel(
            new Markup($"[bold cyan]oras[/] — OCI Registry As Storage\n[dim]Version {GetVersion()}[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(foreground: Color.Cyan1)
        };
        AnsiConsole.Write(headerPanel);
        AnsiConsole.WriteLine();

        // Connected registries
        var config = await _configStore.LoadAsync(cancellationToken);
        var registries = config.Auths.Keys.ToList();
        
        if (registries.Any())
        {
            var registryTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[yellow]Connected Registries[/]").Centered());

            foreach (var registry in registries)
            {
                var hasCredentials = await _credentialService.GetCredentialsAsync(registry, cancellationToken) != null;
                var status = hasCredentials ? "[green]● logged in[/]" : "[grey]○ not authenticated[/]";
                registryTable.AddRow($"{registry} {status}");
            }

            AnsiConsole.Write(registryTable);
            AnsiConsole.WriteLine();
        }
        else
        {
            PromptHelper.ShowInfo("No connected registries. Use Login to authenticate.");
            AnsiConsole.WriteLine();
        }

        // Quick actions menu
        var actions = new[]
        {
            "Browse Registry",
            "Login",
            "Push Artifact",
            "Pull Artifact",
            "Copy Artifact",
            "Tag Artifact",
            "Quit"
        };

        var action = PromptHelper.PromptSelection(
            "[green]Select an action:[/]",
            actions);

        return await HandleActionAsync(action, registries, cancellationToken);
    }

    private async Task<bool> HandleActionAsync(string action, List<string> registries, CancellationToken cancellationToken)
    {
        try
        {
            switch (action)
            {
                case "Browse Registry":
                    var browser = new RegistryBrowser(_serviceProvider);
                    await browser.RunAsync(cancellationToken);
                    return true;

                case "Login":
                    await HandleLoginAsync(cancellationToken);
                    return true;

                case "Push Artifact":
                    PromptHelper.ShowInfo("Push functionality requires command-line arguments. Use: oras push <reference> <files>");
                    AnsiConsole.WriteLine();
                    PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
                    return true;

                case "Pull Artifact":
                    PromptHelper.ShowInfo("Pull functionality requires command-line arguments. Use: oras pull <reference>");
                    AnsiConsole.WriteLine();
                    PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
                    return true;

                case "Copy Artifact":
                    PromptHelper.ShowInfo("Copy functionality requires command-line arguments. Use: oras copy <source> <target>");
                    AnsiConsole.WriteLine();
                    PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
                    return true;

                case "Tag Artifact":
                    PromptHelper.ShowInfo("Tag functionality requires command-line arguments. Use: oras tag <reference> <tags>");
                    AnsiConsole.WriteLine();
                    PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
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
                registry, username, password, false, false, cancellationToken);

            if (isValid)
            {
                await _credentialService.StoreCredentialsAsync(registry, username, password, cancellationToken);
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

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.1.0";
    }
}
