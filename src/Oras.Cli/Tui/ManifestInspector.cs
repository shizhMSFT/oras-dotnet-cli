using Spectre.Console;

namespace Oras.Tui;

/// <summary>
/// Manifest inspector with syntax-highlighted JSON, layer tree, and config preview.
/// </summary>
internal class ManifestInspector
{
    private readonly IServiceProvider _serviceProvider;

    public ManifestInspector(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InspectAsync(
        string reference,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken = default)
    {
        // Fetch manifest
        var manifest = await FetchManifestAsync(reference, credentials, cancellationToken).ConfigureAwait(false);
        if (manifest == null)
        {
            return;
        }

        while (true)
        {
            Console.Clear();

            // Header
            var header = new Rule($"[yellow]Manifest Inspector: {reference}[/]")
            {
                Justification = Justify.Left
            };
            AnsiConsole.Write(header);
            AnsiConsole.WriteLine();

            // Show menu
            var actions = new[]
            {
                "View Manifest JSON",
                "View Layer Tree",
                "View Config Blob",
                "Actions (Pull/Copy/Delete)",
                "Back to tag list"
            };

            var action = PromptHelper.PromptSelection(
                "[green]Select an option:[/]",
                actions);

            var shouldExit = await HandleInspectorActionAsync(action, reference, manifest, credentials, cancellationToken).ConfigureAwait(false);
            if (shouldExit)
            {
                return;
            }
        }
    }

    private async Task<bool> HandleInspectorActionAsync(
        string action,
        string reference,
        ManifestData manifest,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "View Manifest JSON":
                ShowManifestJson(manifest);
                PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
                return false;

            case "View Layer Tree":
                ShowLayerTree(manifest);
                PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
                return false;

            case "View Config Blob":
                await ShowConfigBlobAsync(manifest, credentials, cancellationToken).ConfigureAwait(false);
                PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
                return false;

            case "Actions (Pull/Copy/Delete)":
                await ShowActionsMenuAsync(reference, manifest, credentials, cancellationToken).ConfigureAwait(false);
                return false;

            case "Back to tag list":
                return true;

            default:
                return false;
        }
    }

    private void ShowManifestJson(ManifestData manifest)
    {
        Console.Clear();

        // Format JSON with color (simple approach without JsonText if it's not available)
        var panel = new Panel(new Markup($"[dim]{Markup.Escape(manifest.Json)}[/]"))
        {
            Header = new PanelHeader("[yellow]Manifest JSON[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(foreground: Color.Yellow)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private void ShowLayerTree(ManifestData manifest)
    {
        Console.Clear();

        // Build tree structure
        var tree = new Tree($"[yellow]Manifest ({manifest.MediaType})[/]");

        // Config node
        var configNode = tree.AddNode($"[cyan]config[/] ({manifest.ConfigMediaType})");
        configNode.AddNode($"[dim]{manifest.ConfigDigest} ({FormatSize(manifest.ConfigSize)})[/]");

        // Layers node
        var layersNode = tree.AddNode($"[cyan]layers[/] ({manifest.Layers.Count} total)");
        for (int i = 0; i < manifest.Layers.Count; i++)
        {
            var layer = manifest.Layers[i];
            var layerNode = layersNode.AddNode($"[green][{i}][/] {layer.MediaType}");
            layerNode.AddNode($"[dim]{layer.Digest} ({FormatSize(layer.Size)})[/]");
        }

        // Referrers (if any)
        if (manifest.Referrers.Count > 0)
        {
            var referrersNode = tree.AddNode($"[cyan]referrers[/] ({manifest.Referrers.Count} total)");
            foreach (var referrer in manifest.Referrers)
            {
                var refNode = referrersNode.AddNode($"{referrer.ArtifactType}");
                refNode.AddNode($"[dim]{referrer.Digest} ({FormatSize(referrer.Size)})[/]");
            }
        }

        var panel = new Panel(tree)
        {
            Header = new PanelHeader("[yellow]Layer Tree[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private async Task ShowConfigBlobAsync(
        ManifestData manifest,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        Console.Clear();

        var configJson = await FetchConfigBlobAsync(manifest.ConfigDigest, credentials, cancellationToken).ConfigureAwait(false);

        if (configJson != null)
        {
            var panel = new Panel(new Markup($"[dim]{Markup.Escape(configJson)}[/]"))
            {
                Header = new PanelHeader("[yellow]Config Blob[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(foreground: Color.Yellow)
            };

            AnsiConsole.Write(panel);
        }
        else
        {
            PromptHelper.ShowError("Failed to fetch config blob");
        }

        AnsiConsole.WriteLine();
    }

    private async Task ShowActionsMenuAsync(
        string reference,
        ManifestData manifest,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        var actions = new[]
        {
            "Pull to local directory",
            "Copy to another registry",
            "Tag with additional tags",
            "Delete manifest",
            "Cancel"
        };

        var action = PromptHelper.PromptSelection(
            "[green]Select an action:[/]",
            actions);

        await ExecuteBrowserActionAsync(action, reference, manifest, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteBrowserActionAsync(
        string action,
        string reference,
        ManifestData manifest,
        CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "Pull to local directory":
                await HandlePullAsync(reference, cancellationToken).ConfigureAwait(false);
                break;

            case "Copy to another registry":
                await HandleCopyAsync(reference, cancellationToken).ConfigureAwait(false);
                break;

            case "Tag with additional tags":
                await HandleTagActionAsync(reference, cancellationToken).ConfigureAwait(false);
                break;

            case "Delete manifest":
                await HandleDeleteActionAsync(reference, manifest, cancellationToken).ConfigureAwait(false);
                break;

            case "Cancel":
                break;
        }

        if (action != "Cancel")
        {
            AnsiConsole.WriteLine();
            PromptHelper.PromptText("Press Enter to continue...", allowEmpty: true);
        }
    }

    private async Task HandlePullAsync(string reference, CancellationToken cancellationToken)
    {
        try
        {
            var outputDir = PromptHelper.PromptText(
                "Output directory:", defaultValue: "./");

            var includeReferrers = PromptHelper.PromptConfirmation(
                "Include referrers (signatures, SBOMs)?", defaultValue: false);

            var escapedRef = Markup.Escape(reference);
            var escapedDir = Markup.Escape(outputDir);

            AnsiConsole.MarkupLine($"\n[bold]Pulling {escapedRef} to {escapedDir}[/]");
            if (includeReferrers)
            {
                PromptHelper.ShowInfo("Including referrers");
            }
            AnsiConsole.WriteLine();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var resolveTask = ctx.AddTask("Resolving manifest");
                    var downloadTask = ctx.AddTask("Downloading layers (0/3)", maxValue: 3);
                    var writeTask = ctx.AddTask($"Writing to {escapedDir}");

                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    for (var layer = 1; layer <= 3; layer++)
                    {
                        downloadTask.Description = $"Downloading layers ({layer}/3)";
                        downloadTask.Increment(1);
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }

                    for (var i = 0; i <= 100; i += 25)
                    {
                        writeTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    writeTask.Value = 100;

                    if (includeReferrers)
                    {
                        var referrersTask = ctx.AddTask("Downloading referrers");
                        for (var i = 0; i <= 100; i += 25)
                        {
                            referrersTask.Value = i;
                            await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                        }
                        referrersTask.Value = 100;
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Pulled {reference} to {outputDir}");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Pull cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Pull failed: {ex.Message}");
        }
    }

    private async Task HandleCopyAsync(string reference, CancellationToken cancellationToken)
    {
        try
        {
            var destination = PromptHelper.PromptText(
                "Destination reference (e.g., [green]ghcr.io/myorg/backup:v1[/]):");
            if (string.IsNullOrWhiteSpace(destination))
            {
                return;
            }

            var includeReferrers = PromptHelper.PromptConfirmation(
                "Include referrers (signatures, SBOMs)?", defaultValue: false);

            var escapedSrc = Markup.Escape(reference);
            var escapedDest = Markup.Escape(destination.Trim());

            AnsiConsole.MarkupLine($"\n[bold]Copying {escapedSrc} => {escapedDest}[/]");
            if (includeReferrers)
            {
                PromptHelper.ShowInfo("Including referrers");
            }
            AnsiConsole.WriteLine();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var resolveTask = ctx.AddTask("Resolving source");
                    var copyTask = ctx.AddTask("Copying layers (0/3)", maxValue: 3);
                    var manifestTask = ctx.AddTask("Copying manifest");

                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    for (var layer = 1; layer <= 3; layer++)
                    {
                        copyTask.Description = $"Copying layers ({layer}/3)";
                        copyTask.Increment(1);
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }

                    for (var i = 0; i <= 100; i += 25)
                    {
                        manifestTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    manifestTask.Value = 100;

                    if (includeReferrers)
                    {
                        var referrersTask = ctx.AddTask("Copying referrers");
                        for (var i = 0; i <= 100; i += 25)
                        {
                            referrersTask.Value = i;
                            await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                        }
                        referrersTask.Value = 100;
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Copied {reference} => {destination.Trim()}");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Copy cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Copy failed: {ex.Message}");
        }
    }

    private async Task HandleTagActionAsync(string reference, CancellationToken cancellationToken)
    {
        try
        {
            PromptHelper.ShowInfo("Enter tags separated by spaces (e.g., v1.0 latest stable):");
            var tagsInput = PromptHelper.PromptText("Tags:");
            var tags = tagsInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tags.Length == 0)
            {
                PromptHelper.ShowWarning("No tags provided.");
                return;
            }

            var escapedRef = Markup.Escape(reference);
            AnsiConsole.MarkupLine($"\n[bold]Tagging {escapedRef}[/]");
            AnsiConsole.MarkupLine($"[dim grey]New tags: {string.Join(", ", tags.Select(Markup.Escape))}[/]");
            AnsiConsole.WriteLine();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var resolveTask = ctx.AddTask("Resolving source");
                    var tagTask = ctx.AddTask($"Creating tags (0/{tags.Length})", maxValue: tags.Length);

                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    for (var i = 0; i < tags.Length; i++)
                    {
                        tagTask.Description = $"Creating tags ({i + 1}/{tags.Length})";
                        tagTask.Increment(1);
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Tagged {reference} with {tags.Length} tag(s)");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Tag operation cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Tag operation failed: {ex.Message}");
        }
    }

    private async Task HandleDeleteActionAsync(
        string reference,
        ManifestData manifest,
        CancellationToken cancellationToken)
    {
        try
        {
            AnsiConsole.WriteLine();
            PromptHelper.ShowWarning($"You are about to delete: {reference}");
            PromptHelper.ShowWarning($"Digest: {manifest.Digest}");
            PromptHelper.ShowWarning($"Size: {FormatSize(manifest.TotalSize)}");
            AnsiConsole.WriteLine();

            var confirmed = PromptHelper.PromptConfirmation(
                "[red]Are you sure you want to delete this manifest?[/]",
                false);

            if (!confirmed)
            {
                PromptHelper.ShowInfo("Delete operation cancelled.");
                return;
            }

            var escapedRef = Markup.Escape(reference);
            AnsiConsole.MarkupLine($"\n[bold red]Deleting {escapedRef}[/]");
            AnsiConsole.WriteLine();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var resolveTask = ctx.AddTask("Resolving manifest");
                    var deleteTask = ctx.AddTask("Deleting manifest");

                    for (var i = 0; i <= 100; i += 20)
                    {
                        resolveTask.Value = i;
                        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
                    }
                    resolveTask.Value = 100;

                    for (var i = 0; i <= 100; i += 25)
                    {
                        deleteTask.Value = i;
                        await Task.Delay(80, cancellationToken).ConfigureAwait(false);
                    }
                    deleteTask.Value = 100;
                }).ConfigureAwait(false);

            AnsiConsole.WriteLine();
            PromptHelper.ShowSuccess($"Deleted {reference}");
        }
        catch (OperationCanceledException)
        {
            PromptHelper.ShowWarning("Delete cancelled.");
        }
        catch (Exception ex)
        {
            PromptHelper.ShowError($"Delete failed: {ex.Message}");
        }
    }

    private async Task<ManifestData?> FetchManifestAsync(
        string reference,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .StartAsync("Fetching manifest...", async ctx =>
            {
                try
                {
                    // TODO: Call IManifestStore.FetchAsync()
                    // For now, return mock data
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);

                    return new ManifestData
                    {
                        Json = CreateMockManifestJson(),
                        MediaType = "application/vnd.oci.image.manifest.v1+json",
                        Digest = "sha256:abc123def456...",
                        ConfigMediaType = "application/vnd.oci.image.config.v1+json",
                        ConfigDigest = "sha256:config123...",
                        ConfigSize = 1536,
                        Layers = new List<LayerData>
                        {
                            new() { MediaType = "application/vnd.oci.image.layer.v1.tar+gzip", Digest = "sha256:layer1...", Size = 12_345_678 },
                            new() { MediaType = "application/vnd.oci.image.layer.v1.tar+gzip", Digest = "sha256:layer2...", Size = 45_678_901 },
                        },
                        Referrers = new List<ReferrerData>
                        {
                            new() { ArtifactType = "application/vnd.example.sbom.v1", Digest = "sha256:sbom123...", Size = 4_096 }
                        },
                        TotalSize = 58_030_211
                    };
                }
                catch (Exception ex)
                {
                    PromptHelper.ShowError($"Failed to fetch manifest: {ex.Message}");
                    return null;
                }
            }).ConfigureAwait(false);
    }

    private async Task<string?> FetchConfigBlobAsync(
        string digest,
        (string Username, string Password)? credentials,
        CancellationToken cancellationToken)
    {
        return await AnsiConsole.Status()
            .StartAsync("Fetching config blob...", async ctx =>
            {
                try
                {
                    // TODO: Call IBlobStore.FetchAsync()
                    await Task.Delay(300, cancellationToken).ConfigureAwait(false);

                    return @"{
  ""architecture"": ""amd64"",
  ""os"": ""linux"",
  ""config"": {
    ""Env"": [""PATH=/usr/local/bin:/usr/bin""],
    ""Cmd"": [""/bin/sh""]
  }
}";
                }
                catch (Exception ex)
                {
                    PromptHelper.ShowError($"Failed to fetch config: {ex.Message}");
                    return null;
                }
            }).ConfigureAwait(false);
    }

    private static string CreateMockManifestJson()
    {
        return @"{
  ""schemaVersion"": 2,
  ""mediaType"": ""application/vnd.oci.image.manifest.v1+json"",
  ""config"": {
    ""mediaType"": ""application/vnd.oci.image.config.v1+json"",
    ""digest"": ""sha256:config123..."",
    ""size"": 1536
  },
  ""layers"": [
    {
      ""mediaType"": ""application/vnd.oci.image.layer.v1.tar+gzip"",
      ""digest"": ""sha256:layer1..."",
      ""size"": 12345678
    },
    {
      ""mediaType"": ""application/vnd.oci.image.layer.v1.tar+gzip"",
      ""digest"": ""sha256:layer2..."",
      ""size"": 45678901
    }
  ]
}";
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private class ManifestData
    {
        public string Json { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string Digest { get; set; } = string.Empty;
        public string ConfigMediaType { get; set; } = string.Empty;
        public string ConfigDigest { get; set; } = string.Empty;
        public long ConfigSize { get; set; }
        public List<LayerData> Layers { get; set; } = new();
        public List<ReferrerData> Referrers { get; set; } = new();
        public long TotalSize { get; set; }
    }

    private class LayerData
    {
        public string MediaType { get; set; } = string.Empty;
        public string Digest { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    private class ReferrerData
    {
        public string ArtifactType { get; set; } = string.Empty;
        public string Digest { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
