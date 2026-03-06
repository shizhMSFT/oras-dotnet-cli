using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
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
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var source = parseResult.GetValue(sourceArg)!;
                var targetTags = parseResult.GetValue(targetTagsArg)!;

                // Parse source reference
                var (registry, repository, sourceTag, sourceDigest) = ReferenceHelper.ParseReference(source);

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

}
