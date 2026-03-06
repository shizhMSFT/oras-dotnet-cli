using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Manifest fetch command - fetch a manifest from a registry.
/// </summary>
public static class ManifestFetchCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("fetch", "Fetch a manifest from a registry");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Manifest reference (registry/repository:tag or @digest)"
        };
        command.Add(referenceArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add platform options
        var platformOptions = new PlatformOptions();
        platformOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        // Add descriptor mode option
        var descriptorOpt = new Option<bool>("--descriptor")
        {
            Description = "Output descriptor only (not full manifest)",
            DefaultValueFactory = _ => false
        };
        command.Add(descriptorOpt);

        // Add output file option
        var outputOpt = new Option<string?>("--output", "-o")
        {
            Description = "Write to file instead of stdout",
            DefaultValueFactory = _ => null
        };
        command.Add(outputOpt);

        // Add pretty print option
        var prettyOpt = new Option<bool>("--pretty")
        {
            Description = "Pretty-print JSON output",
            DefaultValueFactory = _ => false
        };
        command.Add(prettyOpt);

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

                var reference = parseResult.GetValue(referenceArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var descriptorMode = parseResult.GetValue(descriptorOpt);
                var output = parseResult.GetValue(outputOpt);
                var pretty = parseResult.GetValue(prettyOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // TODO: Implement using IManifestStore.FetchAsync() or IReferenceFetchable.FetchAsync()
                // If descriptorMode: output descriptor JSON
                // Else: output full manifest JSON
                // If platform specified: resolve index to platform-specific manifest
                // For now, stub with NotImplementedException
                
                throw new NotImplementedException(
                    $"Manifest fetch operation not yet implemented for reference: {reference}");

                // Expected output:
                // --descriptor: JSON descriptor { digest, mediaType, size }
                // Normal: Full manifest JSON (optionally pretty-printed)
            });
        });

        return command;
    }
}
