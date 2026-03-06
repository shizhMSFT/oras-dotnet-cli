using System.CommandLine;
using Oras.Options;
using Oras.Services;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Tag command implementation - create tags for existing manifests.
/// </summary>
internal static class TagCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("tag", "Create a tag for an existing manifest");

        // Add source reference argument
        var sourceArg = new Argument<string>("source")
        {
            Description = "Source reference (registry/repository:tag or @digest)"
        };
        command.Add(sourceArg);

        // Add target tag arguments (one or more)
        var targetTagsArg = new Argument<string[]>("tags")
        {
            Description = "One or more target tags",
            Arity = ArgumentArity.OneOrMore
        };
        command.Add(targetTagsArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetService(typeof(IRegistryService)) as IRegistryService
                    ?? throw new InvalidOperationException("Registry service not available");

                var source = parseResult.GetValue(sourceArg)!;
                var targetTags = parseResult.GetValue(targetTagsArg)!;

                // Parse source reference
                var (registry, repository, sourceTag, sourceDigest) = ParseReference(source);

                // Get remote options
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);

                // TODO: Implement actual tagging using oras-dotnet library
                // This requires IRepository.TagAsync() or ITaggable.TagAsync()
                // For now, stub with NotImplementedException

                foreach (var targetTag in targetTags)
                {
                    // TODO: Call library API
                    // await repository.TagAsync(sourceDigest, targetTag, cancellationToken);
                    throw new NotImplementedException(
                        $"Tag operation not yet implemented. Would tag {source} as {targetTag}");
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

    private static (string registry, string repository, string? tag, string? digest) ParseReference(string reference)
    {
        // Simple reference parser - can be enhanced later
        var parts = reference.Split('/', 2);
        if (parts.Length < 2)
        {
            throw new OrasUsageException(
                $"Invalid reference format: {reference}",
                "Reference must be in format: registry/repository[:tag|@digest]");
        }

        var registry = parts[0];
        var rest = parts[1];

        string? tag = null;
        string? digest = null;
        string repository;

        if (rest.Contains('@'))
        {
            var digestParts = rest.Split('@', 2);
            repository = digestParts[0];
            digest = digestParts[1];
        }
        else if (rest.Contains(':'))
        {
            var tagParts = rest.Split(':', 2);
            repository = tagParts[0];
            tag = tagParts[1];
        }
        else
        {
            repository = rest;
        }

        return (registry, repository, tag, digest);
    }
}
