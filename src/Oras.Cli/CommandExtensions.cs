using System.CommandLine;
using System.CommandLine.Parsing;

namespace Oras;

/// <summary>
/// Extension methods for System.CommandLine 2.x compatibility.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Sets the action for a command using ParseResult.
    /// This wraps the 2.0.3 SetAction API for consistency.
    /// </summary>
    public static void SetAction(this Command command, Func<ParseResult, Task<int>> action)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var exitCode = await action(parseResult).ConfigureAwait(false);
            Environment.ExitCode = exitCode;
        });
    }

    /// <summary>
    /// Gets the value of an argument from the ParseResult.
    /// </summary>
    public static T? GetValue<T>(this ParseResult parseResult, Argument<T> argument)
    {
        return parseResult.GetValue(argument);
    }

    /// <summary>
    /// Gets the value of an option from the ParseResult.
    /// </summary>
    public static T? GetValue<T>(this ParseResult parseResult, Option<T> option)
    {
        return parseResult.GetValue(option);
    }
}
