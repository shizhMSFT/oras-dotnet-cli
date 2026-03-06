using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
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

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var credentialService = serviceProvider.GetRequiredService<ICredentialService>();

                var registry = parseResult.GetValue(registryArg)!;
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var passwordStdin = parseResult.GetValue(remoteOptions.PasswordStdinOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);

                // Normalize registry address
                registry = ReferenceHelper.NormalizeRegistry(registry);

                // Get credentials
                if (passwordStdin)
                {
                    password = await Console.In.ReadLineAsync(cancellationToken).ConfigureAwait(false);
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

                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                try
                {
                    var isValid = await credentialService.ValidateCredentialsAsync(
                        registry,
                        username,
                        password,
                        plainHttp,
                        insecure,
                        registryService,
                        cancellationToken).ConfigureAwait(false);

                    if (!isValid)
                    {
                        throw new OrasAuthenticationException(
                            $"Authentication failed for {registry}",
                            "Check your username and password, or use --password-stdin for secure input.");
                    }

                    // Store credentials
                    await credentialService.StoreCredentialsAsync(
                        registry,
                        username,
                        password,
                        cancellationToken).ConfigureAwait(false);

                    AnsiConsole.MarkupLine($"[green]✓[/] Login succeeded for {registry}");
                    return 0;
                }
                catch (OrasAuthenticationException)
                {
                    throw;
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

}
