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

                // Get remote options
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);

                // Create repository using the source reference
                var repo = await registryService.CreateRepositoryAsync(
                    source,
                    username,
                    password,
                    plainHttp,
                    insecure,
                    cancellationToken).ConfigureAwait(false);

                // Resolve the source to get a descriptor
                var digest = ReferenceHelper.ExtractDigest(source);
                var tag = digest == null ? ReferenceHelper.ExtractTag(source) : null;
                var tagOrDigest = digest ?? tag ?? "latest";

                var descriptor = await repo.ResolveAsync(tagOrDigest, cancellationToken).ConfigureAwait(false);

                // Tag each target
                foreach (var targetTag in targetTags)
                {
                    await repo.TagAsync(descriptor, targetTag, cancellationToken).ConfigureAwait(false);
                    AnsiConsole.MarkupLine($"[green]✓[/] Tagged {Markup.Escape(source)} as {Markup.Escape(targetTag)}");
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }

}
