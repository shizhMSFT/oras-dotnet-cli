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

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

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
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var recursive = parseResult.GetValue(recursiveOpt);
                var concurrency = parseResult.GetValue(concurrencyOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // TODO: Implement using ReadOnlyTargetExtensions.CopyAsync() with CopyOptions
                // This needs both source and destination ITarget instances
                // For now, stub with NotImplementedException

                throw new NotImplementedException(
                    $"Copy operation not yet implemented. Would copy {source} to {destination} " +
                    $"(recursive: {recursive}, concurrency: {concurrency})");

                // Expected output:
                // Text: "Copying 3a1bc987ef01 hello.txt\nCopied  3a1bc987ef01 hello.txt\nCopied [registry] src => dst"
                // JSON: descriptor object
            }).ConfigureAwait(false);
        });

        return command;
    }
}
