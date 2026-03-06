using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
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
                var output = parseResult.GetValue(outputOpt);
                var descriptorMode = parseResult.GetValue(descriptorOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";

                var formatter = FormatOptions.CreateFormatter(format);

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

                if (descriptorMode)
                {
                    // Resolve and output descriptor only
                    var descriptor = await repo.Blobs.ResolveAsync(digest, cancellationToken).ConfigureAwait(false);
                    formatter.WriteDescriptor(new DescriptorResult(
                        descriptor.MediaType,
                        descriptor.Digest,
                        descriptor.Size,
                        descriptor.Annotations as Dictionary<string, string>));
                }
                else
                {
                    // Fetch blob content
                    var (descriptor, stream) = await repo.Blobs.FetchAsync(digest, cancellationToken).ConfigureAwait(false);

                    if (output != null)
                    {
                        // Write to file
                        await using var fileStream = new FileStream(output, FileMode.Create, FileAccess.Write);
                        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Write to stdout
                        await using (stream)
                        {
                            await stream.CopyToAsync(Console.OpenStandardOutput(), cancellationToken).ConfigureAwait(false);
                        }
                    }

                    // In text mode, also print descriptor info
                    if (format == "text")
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Fetched blob {Markup.Escape(descriptor.Digest)}");
                    }
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
