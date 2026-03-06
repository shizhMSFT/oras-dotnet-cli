using Spectre.Console;
using Spectre.Console.Testing;

namespace Oras.Tests.Helpers;

/// <summary>
/// Helper for capturing and asserting Spectre.Console output in tests.
/// </summary>
public sealed class OutputCaptureHelper
{
    private readonly TestConsole _console;

    public OutputCaptureHelper()
    {
        _console = new TestConsole();
    }

    /// <summary>
    /// Gets the underlying Spectre.Console test console.
    /// Use this to pass to components that need an IAnsiConsole.
    /// </summary>
    public IAnsiConsole Console => _console;

    /// <summary>
    /// Gets the captured output as a string.
    /// </summary>
    public string Output => _console.Output;

    /// <summary>
    /// Gets the captured output lines as an array.
    /// </summary>
    public string[] Lines => _console.Lines.ToArray();

    /// <summary>
    /// Clears the captured output.
    /// </summary>
    public void Clear()
    {
        _console.Clear();
    }

    /// <summary>
    /// Checks if the output contains the specified text.
    /// </summary>
    /// <param name="text">The text to search for.</param>
    /// <returns>True if the output contains the text; otherwise, false.</returns>
    public bool Contains(string text)
    {
        return Output.Contains(text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if any line in the output matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to test each line.</param>
    /// <returns>True if any line matches; otherwise, false.</returns>
    public bool AnyLineMatches(Func<string, bool> predicate)
    {
        return Lines.Any(predicate);
    }
}
