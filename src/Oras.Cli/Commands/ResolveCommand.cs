using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Resolve command implementation - resolve a tag to its digest.
/// </summary>
internal static class ResolveCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("resolve", "Resolve a tag to a digest");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Reference to resolve (registry/repository:tag)"
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

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // TODO: Implement using IReferenceFetchable.ResolveAsync() or IResolvable.ResolveAsync()
                // This should return a Descriptor with digest, size, mediaType
                // For now, stub with NotImplementedException

                throw new NotImplementedException(
                    $"Resolve operation not yet implemented for reference: {reference}");

                // Expected output format:
                // Text: sha256:abc123...
                // JSON: { "digest": "sha256:...", "mediaType": "...", "size": 1234 }
            }).ConfigureAwait(false);
        });

        return command;
    }
}
