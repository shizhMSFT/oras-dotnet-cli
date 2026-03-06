using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Copy command implementation - copy artifacts between registries.
/// </summary>
internal static class CopyCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("copy", "Copy artifacts between registries");

        // Add source reference argument
        var sourceArg = new Argument<string>("source")
        {
            Description = "Source reference (registry/repository:tag)"
        };
        command.Add(sourceArg);

        // Add destination reference argument
        var destArg = new Argument<string>("destination")
        {
            Description = "Destination reference (registry/repository:tag)"
        };
        command.Add(destArg);

        // Add remote options (destination auth)
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add source-specific auth options
        var fromUsernameOpt = new Option<string?>("--from-username")
        {
            Description = "Source registry username"
        };
        command.Add(fromUsernameOpt);

        var fromPasswordOpt = new Option<string?>("--from-password")
        {
            Description = "Source registry password or identity token"
        };
        command.Add(fromPasswordOpt);

        // Add platform options
        var platformOptions = new PlatformOptions();
        platformOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        // Add copy-specific options
        var recursiveOpt = new Option<bool>("--recursive", "-r")
        {
            Description = "Recursively copy all referenced artifacts",
            DefaultValueFactory = _ => false
        };
        command.Add(recursiveOpt);

        var concurrencyOpt = new Option<int>("--concurrency")
        {
            Description = "Number of concurrent transfers",
            DefaultValueFactory = _ => 5
        };
        command.Add(concurrencyOpt);

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

                var source = parseResult.GetValue(sourceArg)!;
                var destination = parseResult.GetValue(destArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var fromUsername = parseResult.GetValue(fromUsernameOpt);
                var fromPassword = parseResult.GetValue(fromPasswordOpt);
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var recursive = parseResult.GetValue(recursiveOpt);
                var concurrency = parseResult.GetValue(concurrencyOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // Validate source reference
                ValidateReference(source, "source");
                ValidateReference(destination, "destination");

                // TODO: Replace simulation with actual library calls:
                // 1. Create source Repository via registryService.CreateRepositoryAsync(source, fromUsername, fromPassword, plainHttp, insecure)
                // 2. Create destination Repository via registryService.CreateRepositoryAsync(destination, username, password, plainHttp, insecure)
                // 3. Build CopyOptions { Recursive = recursive, Concurrency = concurrency }
                // 4. If platform is set, configure platform selector on CopyOptions
                // 5. Call ReadOnlyTargetExtensions.CopyAsync(sourceRepo, destination, sourceTag, copyOptions, cancellationToken)

                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("blue"))
                    .StartAsync($"Copying {source} => {destination}...", async ctx =>
                    {
                        ctx.Status($"Resolving source: {source}");
                        await Task.Delay(200).ConfigureAwait(false);

                        ctx.Status("Copying manifests and layers...");
                        if (recursive)
                        {
                            ctx.Status("Copying referrers (signatures, SBOMs)...");
                        }
                        await Task.Delay(300).ConfigureAwait(false);

                        ctx.Status("Verifying destination...");
                        await Task.Delay(100).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                var summary = new CopyResult(
                    source,
                    destination,
                    recursive,
                    concurrency,
                    platform ?? "(all)",
                    "simulated");

                if (format == "json")
                {
                    formatter.WriteObject(summary, OutputJsonContext.Default.CopyResult);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]Copied[/] {source} => {destination}");
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

    /// <summary>
    /// Validates that a reference has the expected format: registry/repository[:tag|@digest]
    /// </summary>
    internal static void ValidateReference(string reference, string paramName)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new OrasUsageException(
                $"The {paramName} reference cannot be empty",
                $"Provide a valid reference (e.g., ghcr.io/myorg/myrepo:v1.0)");
        }

        if (!reference.Contains('/'))
        {
            throw new OrasUsageException(
                $"Invalid {paramName} reference '{reference}': must contain registry and repository path",
                $"Use the format: registry/repository[:tag|@sha256:digest]");
        }
    }
}
