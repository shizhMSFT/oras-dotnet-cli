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
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

                // Create repository
                var repo = await registryService.CreateRepositoryAsync(
                    reference,
                    username,
                    password,
                    plainHttp,
                    insecure,
                    cancellationToken).ConfigureAwait(false);

                // Extract tag or digest from reference
                var digest = ReferenceHelper.ExtractDigest(reference);
                var tag = digest == null ? ReferenceHelper.ExtractTag(reference) : null;
                var tagOrDigest = digest ?? tag ?? "latest";

                // Resolve the reference
                var descriptor = await repo.ResolveAsync(tagOrDigest, cancellationToken).ConfigureAwait(false);

                // Output based on format
                if (format == "json")
                {
                    formatter.WriteDescriptor(new DescriptorResult(
                        descriptor.MediaType,
                        descriptor.Digest,
                        descriptor.Size,
                        descriptor.Annotations as Dictionary<string, string>));
                }
                else
                {
                    Console.WriteLine(descriptor.Digest);
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
