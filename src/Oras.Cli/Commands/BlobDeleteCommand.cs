using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Blob delete command - delete a blob from a registry.
/// </summary>
internal static class BlobDeleteCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("delete", "Delete a blob from a registry");

        // Add reference argument (must include digest)
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Blob reference with digest (registry/repository@digest)"
        };
        command.Add(referenceArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        // Add force option (required in non-interactive mode)
        var forceOpt = new Option<bool>("--force", "-f")
        {
            Description = "Force deletion without confirmation",
            DefaultValueFactory = _ => false
        };
        command.Add(forceOpt);

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

                var reference = parseResult.GetValue(referenceArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var force = parseResult.GetValue(forceOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // Check if we need confirmation
                if (!force && formatter.SupportsInteractivity)
                {
                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete blob {reference}?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Deletion cancelled[/]");
                        return 0;
                    }
                }
                else if (!force)
                {
                    throw new OrasUsageException(
                        "Deletion requires --force flag in non-interactive mode",
                        "Use --force to confirm deletion or run in an interactive terminal.");
                }

                // TODO: Implement using IDeletable.DeleteAsync() or IBlobStore.DeleteAsync()
                // For now, stub with NotImplementedException

                throw new NotImplementedException(
                    $"Blob delete operation not yet implemented for reference: {reference}");

                // Expected output:
                // Text: "Deleted <digest>"
                // JSON: success status
            }).ConfigureAwait(false);
        });

        return command;
    }
}
