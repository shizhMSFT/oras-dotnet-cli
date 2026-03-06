using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Repository ls command - list repositories in a registry.
/// </summary>
internal static class RepoLsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("ls", "List repositories in a registry");

        // Add registry argument
        var registryArg = new Argument<string>("registry")
        {
            Description = "Registry hostname (e.g., docker.io, ghcr.io)"
        };
        command.Add(registryArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        // Add pagination option
        var lastOpt = new Option<string?>("--last")
        {
            Description = "Start listing after this repository name (pagination marker)",
            DefaultValueFactory = _ => null
        };
        command.Add(lastOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var registry = parseResult.GetValue(registryArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var last = parseResult.GetValue(lastOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // TODO: Implement using IRegistry.ListRepositoriesAsync()
                // Returns IAsyncEnumerable<string> of repository names
                // For now, stub with NotImplementedException

                throw new NotImplementedException(
                    $"Repository list operation not yet implemented for registry: {registry}");

                // Expected output:
                // Text: one repository name per line
                // JSON: array of repository names
            }).ConfigureAwait(false);
        });

        return command;
    }
}
