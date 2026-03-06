using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Spectre.Console;

namespace Oras.Output;

/// <summary>
/// JSON-based formatter for machine-readable output (--format json)
/// Each command outputs one JSON object per line
/// </summary>
internal sealed class JsonFormatter : IOutputFormatter
{
    private readonly IAnsiConsole _console;

    public JsonFormatter(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public bool SupportsInteractivity => false;

    public void WriteStatus(string message)
    {
        var result = new StatusResult("success", message);
        WriteObject(result, OutputJsonContext.Default.StatusResult);
    }

    public void WriteError(string message, string? recommendation = null)
    {
        var result = new ErrorResult("error", message, recommendation);
        WriteObject(result, OutputJsonContext.Default.ErrorResult);
    }

    public void WriteDescriptor(DescriptorResult descriptor)
    {
        WriteObject(descriptor, OutputJsonContext.Default.DescriptorResult);
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

        var result = new TableResult(items);
        WriteObject(result, OutputJsonContext.Default.TableResult);
    }

    public void WriteTree(TreeNode root)
    {
        var result = ConvertTree(root);
        WriteObject(result, OutputJsonContext.Default.TreeNodeResult);
    }

    public void WriteJson(string json, bool pretty = false)
    {
        // Already JSON, just output it
        _console.WriteLine(json);
    }

    public void WriteObject<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        var json = JsonSerializer.Serialize(value, jsonTypeInfo);
        _console.WriteLine(json);
    }

    private static TreeNodeResult ConvertTree(TreeNode node)
    {
        var children = node.Children.Count > 0
            ? node.Children.Select(ConvertTree).ToArray()
            : Array.Empty<TreeNodeResult>();

        return new TreeNodeResult(node.Label, node.Metadata, children);
    }
}
