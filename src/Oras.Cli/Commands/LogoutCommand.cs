using System.CommandLine;
using Oras.Services;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Logout command implementation.
/// </summary>
public static class LogoutCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("logout", "Log out from a remote registry");

        // Add registry argument
        var registryArg = new Argument<string>("registry")
        {
            Description = "Registry hostname (e.g., docker.io, ghcr.io)"
        };
        command.Add(registryArg);

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var credentialService = serviceProvider.GetService(typeof(ICredentialService)) as ICredentialService
                    ?? throw new InvalidOperationException("Credential service not available");

                var registry = parseResult.GetValue(registryArg)!;

                // Normalize registry address
                registry = NormalizeRegistry(registry);

                // Remove credentials
                await credentialService.RemoveCredentialsAsync(registry, CancellationToken.None);

                AnsiConsole.MarkupLine($"[green]✓[/] Logout succeeded for {registry}");
                return 0;
            });
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
