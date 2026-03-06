using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Spectre.Console;
using OrasProject.Oras.Oci;

namespace Oras.Commands;

/// <summary>
/// Pull command implementation.
/// </summary>
internal static class PullCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("pull", "Pull files from a remote registry");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Source reference (e.g., registry/repository:tag)"
        };
        command.Add(referenceArg);

        // Add options
        var remoteOptions = new RemoteOptions();
        var platformOptions = new PlatformOptions();
        remoteOptions.ApplyTo(command);
        platformOptions.ApplyTo(command);

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output directory for pulled files",
            DefaultValueFactory = _ => "."
        };
        command.Add(outputOption);

        var keepOldFilesOption = new Option<bool>("--keep-old-files")
        {
            Description = "Do not overwrite existing files"
        };
        command.Add(keepOldFilesOption);

        var concurrencyOption = new Option<int>("--concurrency")
        {
            Description = "Maximum number of concurrent operations",
            DefaultValueFactory = _ => 3
        };
        command.Add(concurrencyOption);

        command.SetAction(async parseResult =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

                var reference = parseResult.GetValue(referenceArg)!;
                var outputDir = parseResult.GetValue(outputOption)!;
                var platform = parseResult.GetValue(platformOptions.PlatformOption);
                var keepOldFiles = parseResult.GetValue(keepOldFilesOption);
                var concurrency = parseResult.GetValue(concurrencyOption);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);

                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                AnsiConsole.MarkupLine($"[blue]Pulling from {reference}...[/]");

                var repo = await registryService.CreateRepositoryAsync(
                    reference,
                    username,
                    password,
                    plainHttp,
                    insecure).ConfigureAwait(false);

                // Resolve the tag or digest
                var tag = ExtractTag(reference);
                var digest = ExtractDigest(reference);

                Descriptor manifestDescriptor;
                if (!string.IsNullOrEmpty(digest))
                {
                    manifestDescriptor = new Descriptor { Digest = digest, MediaType = "application/vnd.oci.image.manifest.v1+json", Size = 0 };
                }
                else if (!string.IsNullOrEmpty(tag))
                {
                    manifestDescriptor = await repo.ResolveAsync(tag).ConfigureAwait(false);
                }
                else
                {
                    throw new OrasUsageException(
                        "No tag or digest specified in reference",
                        "Specify a tag (e.g., :latest) or digest (e.g., @sha256:...)");
                }

                AnsiConsole.MarkupLine($"[dim]Manifest digest: {manifestDescriptor.Digest}[/]");

                // TODO: Implement manifest and layer fetching with OrasProject.Oras v0.5.0 API
                // The Manifests.FetchAsync and Blobs.FetchAsync signatures differ from expected
                throw new NotImplementedException(
                    "Pull operation needs OrasProject.Oras v0.5.0 API integration. " +
                    "The Manifests.FetchAsync API signature differs from expected.");

                // Pull each layer
                // foreach (var layer in manifest.Layers)
                // {
                //     var fileName = ...
                //     var filePath = Path.Combine(outputDir, fileName);
                //     ...
                // }

                // AnsiConsole.MarkupLine($"[green]✓[/] Pulled {reference}");
                // return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

    private static string? ExtractTag(string reference)
    {
        var colonIndex = reference.LastIndexOf(':');
        var slashIndex = reference.LastIndexOf('/');
        var atIndex = reference.IndexOf('@');

        if (atIndex > 0)
        {
            return null; // Has digest, no tag
        }

        if (colonIndex > slashIndex && colonIndex >= 0)
        {
            return reference[(colonIndex + 1)..];
        }

        return "latest"; // Default tag
    }

    private static string? ExtractDigest(string reference)
    {
        var atIndex = reference.IndexOf('@');
        if (atIndex > 0)
        {
            return reference[(atIndex + 1)..];
        }

        return null;
    }
}
