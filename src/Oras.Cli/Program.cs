using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Commands;
using Oras.Services;

namespace Oras;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        services.AddOrasServices();
        var serviceProvider = services.BuildServiceProvider();

        // Create root command
        var rootCommand = new RootCommand("oras - OCI Registry As Storage CLI")
        {
            VersionCommand.Create(),
            LoginCommand.Create(serviceProvider),
            LogoutCommand.Create(serviceProvider),
            PushCommand.Create(serviceProvider),
            PullCommand.Create(serviceProvider)
        };

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }
}
