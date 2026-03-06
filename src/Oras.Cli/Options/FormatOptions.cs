using System.CommandLine;
using Oras.Output;
using Spectre.Console;

namespace Oras.Options;

/// <summary>
/// Options for output formatting.
/// </summary>
public class FormatOptions
{
    public Option<string> FormatOption { get; }
    public Option<bool> PrettyOption { get; }

    public FormatOptions()
    {
        FormatOption = new Option<string>("--format", "-f")
        {
            Description = "Output format (text, json)",
            DefaultValueFactory = _ => "text"
        };
        FormatOption.AcceptOnlyFromAmong("text", "json");

        PrettyOption = new Option<bool>("--pretty")
        {
            Description = "Pretty-print output"
        };
    }

    public void ApplyTo(Command command)
    {
        command.Add(FormatOption);
        command.Add(PrettyOption);
    }

    /// <summary>
    /// Create an IOutputFormatter based on the format option value
    /// </summary>
    public static IOutputFormatter CreateFormatter(string format, IAnsiConsole? console = null)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonFormatter(console),
            "text" => new TextFormatter(console),
            _ => new TextFormatter(console)
        };
    }
}
