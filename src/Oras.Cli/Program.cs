using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Commands;
using Oras.Services;
using Oras.Tui;

namespace Oras;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Set up dependency injection
        var services = new ServiceCollection();
        services.AddOrasServices();
        await using var serviceProvider = services.BuildServiceProvider();

        // S3-01: Launch TUI dashboard if no args in TTY mode
        if (Dashboard.ShouldLaunchTui(args))
        {
            var dashboard = new Dashboard(serviceProvider);
            return await dashboard.RunAsync().ConfigureAwait(false);
        }

        // Create root command
        var rootCommand = new RootCommand("oras - OCI Registry As Storage CLI")
        {
            // Core commands
            VersionCommand.Create(),
            LoginCommand.Create(serviceProvider),
            LogoutCommand.Create(serviceProvider),
            PushCommand.Create(serviceProvider),
            PullCommand.Create(serviceProvider),
            
            // Sprint 2 - P0 commands
            TagCommand.Create(serviceProvider),
            ResolveCommand.Create(serviceProvider),
            CopyCommand.Create(serviceProvider),
            
            // Sprint 2 - P1 commands
            AttachCommand.Create(serviceProvider),
            DiscoverCommand.Create(serviceProvider),
            
            // Backup/restore commands
            BackupCommand.Create(serviceProvider),
            RestoreCommand.Create(serviceProvider),
            
            // Subcommand groups
            CreateRepoCommand(serviceProvider),
            CreateBlobCommand(serviceProvider),
            CreateManifestCommand(serviceProvider)
        };

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }

    private static Command CreateRepoCommand(IServiceProvider serviceProvider)
    {
        var repoCommand = new Command("repo", "Repository operations");
        repoCommand.Add(RepoLsCommand.Create(serviceProvider));
        repoCommand.Add(RepoTagsCommand.Create(serviceProvider));
        return repoCommand;
    }

    private static Command CreateBlobCommand(IServiceProvider serviceProvider)
    {
        var blobCommand = new Command("blob", "Blob operations");
        blobCommand.Add(BlobFetchCommand.Create(serviceProvider));
        blobCommand.Add(BlobPushCommand.Create(serviceProvider));
        blobCommand.Add(BlobDeleteCommand.Create(serviceProvider));
        return blobCommand;
    }

    private static Command CreateManifestCommand(IServiceProvider serviceProvider)
    {
        var manifestCommand = new Command("manifest", "Manifest operations");
        manifestCommand.Add(ManifestFetchCommand.Create(serviceProvider));
        manifestCommand.Add(ManifestPushCommand.Create(serviceProvider));
        manifestCommand.Add(ManifestDeleteCommand.Create(serviceProvider));
        manifestCommand.Add(ManifestFetchConfigCommand.Create(serviceProvider));
        return manifestCommand;
    }
}
