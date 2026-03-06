# Output System Usage Guide

This guide shows how to use the output formatting system implemented in Sprint 1 (S1-03, S1-15).

## Basic Output Formatting

### Getting a Formatter

```csharp
using Oras.Options;
using Oras.Output;

// In a command handler
string format = parseResult.GetValue(formatOptions.FormatOption);
IOutputFormatter formatter = FormatOptions.CreateFormatter(format);
```

### Writing Status Messages

```csharp
formatter.WriteStatus("Login Succeeded");
formatter.WriteStatus("Pulled localhost:5000/hello:latest");
```

### Writing Errors

```csharp
formatter.WriteError(
    "Failed to authenticate with registry",
    "Check your credentials and try again with 'oras login'"
);
```

### Writing Tables

```csharp
var headers = new[] { "Name", "Digest", "Size" };
var rows = new[]
{
    new[] { "hello.txt", "sha256:abc123...", "1.2 KB" },
    new[] { "config.json", "sha256:def456...", "512 B" }
};
formatter.WriteTable(headers, rows);
```

### Writing Trees

```csharp
var root = new TreeNode
{
    Label = "manifest (application/vnd.oci.image.manifest.v1+json)",
    Children = new List<TreeNode>
    {
        new TreeNode
        {
            Label = "config",
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "application/vnd.oci.image.config.v1+json",
                ["size"] = "1.5 KB"
            }
        },
        new TreeNode
        {
            Label = "layers",
            Children = new List<TreeNode>
            {
                new TreeNode
                {
                    Label = "sha256:d4e5f6...",
                    Metadata = new Dictionary<string, string>
                    {
                        ["size"] = "12.3 MB"
                    }
                }
            }
        }
    }
};
formatter.WriteTree(root);
```

### Writing JSON

```csharp
// For pre-formatted JSON strings
string manifestJson = await GetManifestJsonAsync();
formatter.WriteJson(manifestJson, pretty: true);

// For objects that need serialization
var descriptor = new
{
    MediaType = "application/vnd.oci.image.manifest.v1+json",
    Digest = "sha256:abc123...",
    Size = 512
};
formatter.WriteDescriptor(descriptor);
```

## Progress Rendering

### For Push/Pull Operations

```csharp
using Oras.Output;

// Create a progress renderer
using var progressRenderer = new ProgressRenderer();

// Start the operation
progressRenderer.Start("Pushing to localhost:5000/myapp:latest", totalLayers: 3);

// For each layer
progressRenderer.OnLayerStart("sha256:3a1bc987...", "hello.txt", size: 1_200_000);

// Update progress as bytes are transferred
progressRenderer.OnLayerProgress("sha256:3a1bc987...", bytesTransferred: 600_000, totalBytes: 1_200_000);

// Complete the layer
progressRenderer.OnLayerComplete("sha256:3a1bc987...", "hello.txt", size: 1_200_000);

// Complete the overall operation
progressRenderer.Complete("Pushed localhost:5000/myapp:latest");
```

### Integrating with oras-dotnet Library

```csharp
using Oras.Output;
using OrasProject.Oras.Content;

// Create adapter for library callbacks
using var progressRenderer = new ProgressRenderer();
var adapter = new ProgressCallbackAdapter(progressRenderer);

progressRenderer.Start("Copying artifact", totalLayers: 5);

// When calling library copy operations, hook into PreCopy/PostCopy callbacks
var copyOptions = new CopyGraphOptions
{
    PreCopy = async (ctx, desc) =>
    {
        // Called before each blob/layer is copied
        adapter.OnLayerStart(desc.Digest, desc.Annotations?["org.opencontainers.image.title"], desc.Size);
        return Task.CompletedTask;
    },
    PostCopy = async (ctx, desc) =>
    {
        // Called after each blob/layer is copied
        adapter.OnLayerComplete(desc.Digest, desc.Annotations?["org.opencontainers.image.title"], desc.Size);
        return Task.CompletedTask;
    }
};

// Perform the copy
await sourceRepo.CopyAsync(targetRepo, manifestDescriptor, copyOptions, cancellationToken);

progressRenderer.Complete("Copy complete");
```

## TTY Detection

The formatters automatically detect TTY vs non-TTY environments:

- **TTY (terminal):** Styled output with colors, progress bars, tables
- **Non-TTY (pipe/redirect):** Plain text, no ANSI codes, simple line-by-line output

### Checking for Interactivity

```csharp
if (formatter.SupportsInteractivity)
{
    // Show progress bars, prompts, etc.
}
else
{
    // Use simple status messages
}
```

## JSON Mode Output

When `--format json` is specified, all output is structured JSON:

### Status
```json
{"status":"success","message":"Login Succeeded"}
```

### Errors
```json
{"status":"error","error":"Authentication failed","recommendation":"Run 'oras login' first"}
```

### Tables
```json
{"items":[{"Name":"hello.txt","Digest":"sha256:abc123","Size":"1.2 KB"}]}
```

### Trees
```json
{
  "label":"manifest",
  "metadata":{"type":"application/vnd.oci.image.manifest.v1+json"},
  "children":[{"label":"config","metadata":{"size":"1.5 KB"}}]
}
```

## Best Practices

1. **Always escape user input:** The formatters handle this automatically, but if you build markup manually, use `Markup.Escape()`

2. **Use appropriate methods:** Don't write raw strings; use the semantic methods (`WriteStatus`, `WriteError`, `WriteTable`, etc.)

3. **Test both modes:** Verify your command works with both `--format text` and `--format json`

4. **Clean up progress:** Always use `using` with `ProgressRenderer` to ensure proper disposal

5. **Provide recommendations:** When writing errors, always include a recommendation for what the user should do next

## Future: TUI Mode (Sprint 3)

In Sprint 3, these same abstractions will power the interactive TUI:
- `TreeNode` will render as interactive Spectre.Console `Tree` widgets in the browser
- `ProgressRenderer` will be enhanced with live updating displays
- `IOutputFormatter` will gain a `TuiFormatter` implementation for dashboard views
