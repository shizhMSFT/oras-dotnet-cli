using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Blob fetch command - fetch a blob by digest.
/// </summary>
internal static class BlobFetchCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("fetch", "Fetch a blob by digest");

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

        // Add output file option
        var outputOpt = new Option<string?>("--output", "-o")
        {
            Description = "Write to file instead of stdout",
            DefaultValueFactory = _ => null
        };
        command.Add(outputOpt);

        // Add descriptor mode option
        var descriptorOpt = new Option<bool>("--descriptor")
        {
            Description = "Output descriptor only (not blob content)",
            DefaultValueFactory = _ => false
        };
        command.Add(descriptorOpt);

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

                var reference = parseResult.GetValue(referenceArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var output = parseResult.GetValue(outputOpt);
                var descriptorMode = parseResult.GetValue(descriptorOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // TODO: Implement using IBlobStore.FetchAsync()
                // If descriptorMode: output descriptor JSON
                // Else: stream blob to stdout or file
                // For now, stub with NotImplementedException

                throw new NotImplementedException(
                    $"Blob fetch operation not yet implemented for reference: {reference}");

                // Expected output:
                // --descriptor: JSON descriptor { digest, size, mediaType }
                // Normal: raw blob content to stdout or file
            }).ConfigureAwait(false);
        });

        return command;
    }
}
