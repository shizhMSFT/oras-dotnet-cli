using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Oras.Options;
using Oras.Services;
using Oras.Output;
using Spectre.Console;
using OrasProject.Oras.Oci;

namespace Oras.Commands;

/// <summary>
/// Discover command implementation - discover referrers of a manifest.
/// </summary>
internal static class DiscoverCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("discover", "Discover referrers of a manifest");

        // Add reference argument
        var referenceArg = new Argument<string>("reference")
        {
            Description = "Target manifest reference (registry/repository:tag or @digest)"
        };
        command.Add(referenceArg);

        // Add remote options
        var remoteOptions = new RemoteOptions();
        remoteOptions.ApplyTo(command);

        // Add format options
        var formatOptions = new FormatOptions();
        formatOptions.ApplyTo(command);

        // Add artifact type filter
        var artifactTypeOpt = new Option<string?>("--artifact-type")
        {
            Description = "Filter by artifact type",
            DefaultValueFactory = _ => null
        };
        command.Add(artifactTypeOpt);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ErrorHandler.HandleAsync(async () =>
            {
                var registryService = serviceProvider.GetRequiredService<IRegistryService>();

                var reference = parseResult.GetValue(referenceArg)!;
                var plainHttp = parseResult.GetValue(remoteOptions.PlainHttpOption);
                var insecure = parseResult.GetValue(remoteOptions.InsecureOption);
                var artifactTypeFilter = parseResult.GetValue(artifactTypeOpt);
                var format = parseResult.GetValue(formatOptions.FormatOption) ?? "text";
                var username = parseResult.GetValue(remoteOptions.UsernameOption);
                var password = parseResult.GetValue(remoteOptions.PasswordOption);

                var formatter = FormatOptions.CreateFormatter(format);

                // Create repository and resolve
                var repo = await registryService.CreateRepositoryAsync(
                    reference, username, password, plainHttp, insecure, cancellationToken).ConfigureAwait(false);

                var resolveRef = ReferenceHelper.ExtractDigest(reference) ?? ReferenceHelper.ExtractTag(reference) ?? "latest";
                var descriptor = await repo.ResolveAsync(resolveRef, cancellationToken).ConfigureAwait(false);

                // Fetch referrers
                var referrers = new List<Descriptor>();
                if (!string.IsNullOrEmpty(artifactTypeFilter))
                {
                    await foreach (var referrer in repo.FetchReferrersAsync(descriptor, artifactTypeFilter, cancellationToken).ConfigureAwait(false))
                    {
                        referrers.Add(referrer);
                    }
                }
                else
                {
                    await foreach (var referrer in repo.FetchReferrersAsync(descriptor, cancellationToken).ConfigureAwait(false))
                    {
                        referrers.Add(referrer);
                    }
                }

                if (format == "json")
                {
                    var results = referrers.Select(r => new DescriptorResult(
                        r.MediaType, r.Digest, r.Size, r.Annotations as Dictionary<string, string>)).ToArray();
                    formatter.WriteObject(new DiscoverResult(reference, results), OutputJsonContext.Default.DiscoverResult);
                }
                else
                {
                    // Build tree for text output
                    var root = new Output.TreeNode { Label = reference };
                    
                    // Group by artifact type (use MediaType for grouping)
                    var grouped = referrers.GroupBy(r => r.MediaType);
                    foreach (var group in grouped)
                    {
                        var typeNode = new Output.TreeNode { Label = group.Key };
                        foreach (var referrer in group)
                        {
                            var childNode = new Output.TreeNode
                            {
                                Label = referrer.Digest,
                                Metadata = new Dictionary<string, string>
                                {
                                    ["size"] = FormatHelper.FormatSize(referrer.Size)
                                }
                            };
                            typeNode.Children.Add(childNode);
                        }
                        root.Children.Add(typeNode);
                    }

                    formatter.WriteTree(root);
                }
                return 0;
            }).ConfigureAwait(false);
        });

        return command;
    }
}
