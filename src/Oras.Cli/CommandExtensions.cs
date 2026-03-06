using System.CommandLine;
using System.CommandLine.Parsing;

namespace Oras;

/// <summary>
/// Extension methods for System.CommandLine 2.x compatibility.
/// </summary>
internal static class CommandExtensions
{
    /// <summary>
    /// Sets the action for a command using ParseResult.
    /// This wraps the 2.0.3 SetAction API for consistency.
    /// </summary>
    public static void SetAction(this Command command, Func<ParseResult, CancellationToken, Task<int>> action)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var exitCode = await action(parseResult, cancellationToken).ConfigureAwait(false);
            Environment.ExitCode = exitCode;
        });
    }
}
