using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Services;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Logout command implementation.
/// </summary>
internal static class LogoutCommand
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

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var credentialService = serviceProvider.GetRequiredService<ICredentialService>();

                var registry = parseResult.GetValue(registryArg)!;

                // Normalize registry address
                registry = ReferenceHelper.NormalizeRegistry(registry);

                // Remove credentials
                await credentialService.RemoveCredentialsAsync(registry, cancellationToken).ConfigureAwait(false);

                AnsiConsole.MarkupLine($"[green]✓[/] Logout succeeded for {registry}");
                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

}
