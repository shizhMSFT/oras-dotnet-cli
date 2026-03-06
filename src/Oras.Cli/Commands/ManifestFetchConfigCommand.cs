using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Manifest fetch-config command - fetch the config blob referenced by a manifest.
/// </summary>
internal static class ManifestFetchConfigCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("fetch-config", "Fetch the config blob referenced by a manifest");

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

        // Add output file option
        var outputOpt = new Option<string?>("--output", "-o")
        {
            Description = "Write to file instead of stdout",
            DefaultValueFactory = _ => null
        };
        command.Add(outputOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var output = parseResult.GetValue(outputOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // TODO: Implement two-step fetch:
                // 1. Fetch manifest using IManifestStore.FetchAsync()
                // 2. Extract config descriptor from manifest
                // 3. Fetch config blob using IBlobStore.FetchAsync(config descriptor)
                // For now, stub with NotImplementedException

                throw new NotImplementedException(
                    $"Manifest fetch-config operation not yet implemented for reference: {reference}");

                // Expected output:
                // Config blob JSON to stdout or file
            }).ConfigureAwait(false);
        });

        return command;
    }
}
