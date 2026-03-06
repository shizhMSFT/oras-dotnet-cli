using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Login command implementation.
/// </summary>
internal static class LoginCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("login", "Log in to a remote registry");

        // Add registry argument
        var registryArg = new Argument<string>("registry")
        {
            Description = "Registry hostname (e.g., docker.io, ghcr.io)"
        };
        command.Add(registryArg);

        // Add options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var credentialService = serviceProvider.GetService(typeof(ICredentialService)) as ICredentialService
                    ?? throw new InvalidOperationException("Credential service not available");

                var registry = parseResult.GetValue(registryArg)!;
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var passwordStdin = parseResult.GetValue(remoteOptions.PasswordStdinOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);

                // Normalize registry address
                registry = NormalizeRegistry(registry);

                // Get credentials
                if (passwordStdin)
                {
                    password = await Console.In.ReadLineAsync().ConfigureAwait(false);
                }

                if (string.IsNullOrEmpty(username))
                {
                    username = AnsiConsole.Ask<string>("Username:");
                }

                if (string.IsNullOrEmpty(password))
                {
                    password = AnsiConsole.Prompt(
                        new TextPrompt<string>("Password:")
                            .Secret());
                }

                // Validate credentials
                AnsiConsole.Status()
                    .Start("Authenticating...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                    });

                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

                try
                {
                    // Try to create a registry client with the credentials
                    await registryService.CreateRegistryAsync(
                        registry,
                        username,
                        password,
                        plainHttp,
                        insecure,
                        CancellationToken.None).ConfigureAwait(false);

                    // Store credentials
                    await credentialService.StoreCredentialsAsync(
                        registry,
                        username,
                        password,
                        CancellationToken.None).ConfigureAwait(false);

                    AnsiConsole.MarkupLine($"[green]✓[/] Login succeeded for {registry}");
                    return 0;
                }
                catch (Exception ex)
                {
                    throw new OrasAuthenticationException(
                        $"Authentication failed for {registry}: {ex.Message}",
                        "Check your username and password, or use --password-stdin for secure input.");
                }
            }).ConfigureAwait(false);
        });

        return command;
    }

    private static string NormalizeRegistry(string registry)
    {
        // Remove protocol if present
        registry = registry.Replace("https://", "").Replace("http://", "");

        // Remove trailing slash
        registry = registry.TrimEnd('/');

        // Handle Docker Hub special case
        if (registry.Equals("docker.io", StringComparison.OrdinalIgnoreCase))
        {
            return "registry-1.docker.io";
        }

        return registry;
    }
}
