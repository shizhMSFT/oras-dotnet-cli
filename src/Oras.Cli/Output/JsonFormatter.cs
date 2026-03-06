using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Spectre.Console;

namespace Oras.Output;

/// <summary>
/// JSON-based formatter for machine-readable output (--format json)
/// Each command outputs one JSON object per line
/// </summary>
internal sealed class JsonFormatter : IOutputFormatter
{
    private readonly IAnsiConsole _console;
    private readonly JsonSerializerOptions _options;

    public JsonFormatter(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
        _options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public bool SupportsInteractivity => false;

    public void WriteStatus(string message)
    {
        WriteObject(new { status = "success", message });
    }

    public void WriteError(string message, string? recommendation = null)
    {
        object obj;
        if (recommendation != null)
        {
            obj = new { status = "error", error = message, recommendation };
        }
        else
        {
            obj = new { status = "error", error = message, recommendation = (string?)null };
        }
        WriteObject(obj);
    }

    public void WriteDescriptor(object descriptor)
    {
        WriteObject(descriptor);
    }

    public void WriteTable(string[] headers, IEnumerable<string[]> rows)
    {
        var items = rows.Select(row =>
        {
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < headers.Length && i < row.Length; i++)
            {
                dict[headers[i]] = row[i];
            }
            return dict;
        }).ToArray();

        WriteObject(new { items });
    }

    public void WriteTree(TreeNode root)
    {
        WriteObject(ConvertTreeToJson(root));
    }

    public void WriteJson(string json, bool pretty = false)
    {
        // Already JSON, just output it
        _console.WriteLine(json);
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public void WriteObject(object obj)
    {
        var json = JsonSerializer.Serialize(obj, _options);
        _console.WriteLine(json);
    }

    private object ConvertTreeToJson(TreeNode node)
    {
        var obj = new Dictionary<string, object>
        {
            ["label"] = node.Label
        };

        if (node.Metadata.Count > 0)
        {
            obj["metadata"] = node.Metadata;
        }

        if (node.Children.Count > 0)
        {
            obj["children"] = node.Children.Select(ConvertTreeToJson).ToArray();
        }

        return obj;
    }
}
