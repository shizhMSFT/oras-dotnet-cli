using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
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
        var forceOpt = new Option<bool>("--force")
        {
            Description = "Force deletion without confirmation",
            DefaultValueFactory = _ => false
        };
        command.Add(forceOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
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

                // Create repository
                var repo = await registryService.CreateRepositoryAsync(
                    reference, username, password, plainHttp, insecure, cancellationToken).ConfigureAwait(false);

                // Extract digest from reference
                var digest = ReferenceHelper.ExtractDigest(reference);
                if (string.IsNullOrEmpty(digest))
                {
                    throw new OrasUsageException(
                        "Reference must include a digest (@sha256:...)",
                        "Use format: registry/repository@sha256:digest");
                }

                // Resolve descriptor
                var descriptor = await repo.Blobs.ResolveAsync(digest, cancellationToken).ConfigureAwait(false);

                // Delete blob
                await repo.Blobs.DeleteAsync(descriptor, cancellationToken).ConfigureAwait(false);

                // Output result
                if (format == "text")
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Deleted {Markup.Escape(descriptor.Digest)}");
                }
                else
                {
                    formatter.WriteObject(
                        new DeleteResult(reference, "deleted"),
                        OutputJsonContext.Default.DeleteResult);
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
