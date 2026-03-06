using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Spectre.Console;

namespace Oras.Output;

/// <summary>
/// Text-based formatter using Spectre.Console for TTY output with fallback to plain text
/// </summary>
internal sealed class TextFormatter : IOutputFormatter
{
    private readonly IAnsiConsole _console;

    public TextFormatter(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public bool SupportsInteractivity => _console.Profile.Capabilities.Interactive;

    public void WriteStatus(string message)
    {
        if (_console.Profile.Capabilities.Ansi)
        {
            _console.MarkupLine($"[green]{Markup.Escape(message)}[/]");
        }
        else
        {
            _console.WriteLine(message);
        }
    }

    public void WriteError(string message, string? recommendation = null)
    {
        if (_console.Profile.Capabilities.Ansi)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
            if (!string.IsNullOrEmpty(recommendation))
            {
                _console.MarkupLine($"[yellow]Recommendation:[/] {Markup.Escape(recommendation)}");
            }
        }
        else
        {
            _console.WriteLine($"Error: {message}");
            if (!string.IsNullOrEmpty(recommendation))
            {
                _console.WriteLine($"Recommendation: {recommendation}");
            }
        }
    }

    public void WriteDescriptor(DescriptorResult descriptor)
    {
        var json = JsonSerializer.Serialize(descriptor, OutputJsonContext.Default.DescriptorResult);
        WriteJson(json, pretty: true);
    }

    public void WriteTable(string[] headers, IEnumerable<string[]> rows)
    {
        if (_console.Profile.Capabilities.Ansi)
        {
            var table = new Table();
            foreach (var header in headers)
            {
                table.AddColumn(header);
            }

            foreach (var row in rows)
            {
                table.AddRow(row.Select(Markup.Escape).ToArray());
            }

            _console.Write(table);
        }
        else
        {
            // Plain text fallback
            _console.WriteLine(string.Join("\t", headers));
            foreach (var row in rows)
            {
                _console.WriteLine(string.Join("\t", row));
            }
        }
    }

    public void WriteTree(TreeNode root)
    {
        if (_console.Profile.Capabilities.Ansi)
        {
            var tree = BuildSpectreTree(root);
            _console.Write(tree);
        }
        else
        {
            // Plain text fallback with indentation
            WritePlainTree(root, 0);
        }
    }

    public void WriteJson(string json, bool pretty = false)
    {
        if (pretty)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var formatted = FormatJson(doc, indented: true);
                _console.WriteLine(formatted);
                return;
            }
            catch
            {
                _console.WriteLine(json);
                return;
            }
        }

        if (_console.Profile.Capabilities.Ansi)
        {
            // Use Spectre.Console's JSON syntax highlighting
            // TODO: Add Spectre.Console.Json package for JsonText support
            // var jsonText = new JsonText(json);
            // _console.Write(jsonText);
            _console.WriteLine(json);
        }
        else
        {
            _console.WriteLine(json);
        }
    }

    public void WriteObject<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        var json = JsonSerializer.Serialize(value, jsonTypeInfo);
        WriteJson(json, pretty: true);
    }

    private static string FormatJson(JsonDocument document, bool indented)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented });
        JsonSerializer.Serialize(writer, document, OutputJsonContext.Default.JsonDocument);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private Tree BuildSpectreTree(TreeNode node)
    {
        var tree = new Tree(Markup.Escape(node.Label));
        AddTreeNodes(tree, node.Children);
        return tree;
    }

    private void AddTreeNodes(IHasTreeNodes parent, List<TreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            var label = Markup.Escape(node.Label);
            if (node.Metadata.Count > 0)
            {
                var metadata = string.Join(", ", node.Metadata.Select(kv => $"[dim]{kv.Key}:[/] {Markup.Escape(kv.Value)}"));
                label = $"{label} [dim]({metadata})[/]";
            }

            var childNode = parent.AddNode(label);
            AddTreeNodes(childNode, node.Children);
        }
    }

    private void WritePlainTree(TreeNode node, int indent)
    {
        var prefix = new string(' ', indent * 2);
        var metadata = node.Metadata.Count > 0
            ? $" ({string.Join(", ", node.Metadata.Select(kv => $"{kv.Key}: {kv.Value}"))})"
            : string.Empty;

        _console.WriteLine($"{prefix}{(indent > 0 ? "├── " : "")}{node.Label}{metadata}");

        foreach (var child in node.Children)
        {
            WritePlainTree(child, indent + 1);
        }
    }
}
