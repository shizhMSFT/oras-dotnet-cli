using System.CommandLine;
using Spectre.Console;

namespace Oras.Output;

/// <summary>
/// Shared format option for all commands
/// </summary>
public sealed class FormatOptions
{
    private static readonly Option<string> FormatOption = new(
        aliases: new[] { "--format" },
        getDefaultValue: () => "text",
        description: "Output format: text or json");

    static FormatOptions()
    {
        FormatOption.AddCompletions("text", "json");
    }

    public string Format { get; set; } = "text";

    /// <summary>
    /// Apply the format option to a command
    /// </summary>
    public static void ApplyTo(Command command)
    {
        command.AddOption(FormatOption);
    }

    /// <summary>
    /// Bind the format option to a FormatOptions instance
    /// </summary>
    public static FormatOptions Bind(string format)
    {
        return new FormatOptions { Format = format };
    }

    /// <summary>
    /// Create an IOutputFormatter based on the format option
    /// </summary>
    public IOutputFormatter CreateFormatter(IAnsiConsole? console = null)
    {
        return Format.ToLowerInvariant() switch
        {
            "json" => new JsonFormatter(console),
            "text" => new TextFormatter(console),
            _ => new TextFormatter(console)
        };
    }
}
