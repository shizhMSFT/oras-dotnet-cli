using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;

namespace Oras.Commands;

/// <summary>
/// Repository tags command - list tags in a repository.
/// </summary>
internal static class RepoTagsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("tags", "List tags in a repository");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Repository reference (registry/repository)"
        };
        command.Add(referenceArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        // Add pagination option
        var lastOpt = new Option<string?>("--last")
        {
            Description = "Start listing after this tag (pagination marker)",
            DefaultValueFactory = _ => null
        };
        command.Add(lastOpt);

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
                var last = parseResult.GetValue(lastOpt);
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

                // List tags
                var tags = new List<string>();
                await foreach (var tag in repo.ListTagsAsync(last ?? "", cancellationToken).ConfigureAwait(false))
                {
                    tags.Add(tag);
                    if (format == "text")
                    {
                        Console.WriteLine(tag);
                    }
                }

                // Output as JSON if requested
                if (format == "json")
                {
                    formatter.WriteObject(new ListResult(tags.ToArray()), OutputJsonContext.Default.ListResult);
                }

                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
