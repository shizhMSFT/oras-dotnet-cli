using System.CommandLine;

namespace Oras.Options;

/// <summary>
/// Common options available across all commands.
/// </summary>
public class CommonOptions
{
    public Option<bool> DebugOption { get; }
    public Option<bool> VerboseOption { get; }

    public CommonOptions()
    {
        DebugOption = new Option<bool>("--debug", "-d")
        {
            Description = "Enable debug output"
        };

        VerboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Enable verbose output"
        };
    }

    public void ApplyTo(Command command, bool recursive = true)
    {
        command.Add(DebugOption);
        command.Add(VerboseOption);
        
        // Apply recursively to subcommands
        if (recursive)
        {
            foreach (var subcommand in command.Subcommands)
            {
                ApplyTo(subcommand, recursive);
            }
        }
    }

    public static CommonOptions GetValues(ParseResult parseResult, CommonOptions options)
    {
        return options;
    }
}
