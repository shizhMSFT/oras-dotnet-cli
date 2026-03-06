using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using OrasProject.Oras.Registry.Remote;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Version command implementation.
/// </summary>
internal static class VersionCommand
{
    public static Command Create()
    {
        var command = new Command("version", "Show version information");

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                await Task.CompletedTask.ConfigureAwait(false); // Make it async compatible

                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "0.0.0";
                var informationalVersion = assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion ?? version;

                // Get commit SHA if available
                var commitSha = GetCommitSha(assembly);

                // Get .NET runtime version
                var runtimeVersion = RuntimeInformation.FrameworkDescription;

                // Get OS/Platform
                var platform = RuntimeInformation.OSDescription;
                var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("Component")
                    .AddColumn("Version");

                table.AddRow("CLI Version", informationalVersion);
                table.AddRow("Library Version", GetLibraryVersion());
                table.AddRow(".NET Runtime", runtimeVersion);
                table.AddRow("Platform", $"{platform} ({architecture})");

                if (!string.IsNullOrEmpty(commitSha))
                {
                    table.AddRow("Commit", commitSha);
                }

                AnsiConsole.Write(table);

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

    private static string GetLibraryVersion()
    {
        try
        {
            var version = typeof(Repository).Assembly.GetName().Version?.ToString();
            return version ?? "unknown";
        }
        catch
        {
            // Ignore
        }

        return "unknown";
    }

    private static string? GetCommitSha(Assembly assembly)
    {
        try
        {
            var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
            var commitShaAttr = metadata.FirstOrDefault(m => m.Key == "CommitSha");
            return commitShaAttr?.Value;
        }
        catch
        {
            return null;
        }
    }
}
