using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;

namespace Oras.Tests.Helpers;

/// <summary>
/// Helper for testing System.CommandLine commands programmatically.
/// Captures stdout, stderr, and exit codes for assertions.
/// </summary>
public sealed class CommandTestHelper
{
    private readonly StringWriter _standardOutput;
    private readonly StringWriter _standardError;

    public CommandTestHelper()
    {
        _standardOutput = new StringWriter();
        _standardError = new StringWriter();
    }

    /// <summary>
    /// Gets the captured standard output.
    /// </summary>
    public string StandardOutput => _standardOutput.ToString();

    /// <summary>
    /// Gets the captured standard error.
    /// </summary>
    public string StandardError => _standardError.ToString();

    /// <summary>
    /// Gets the exit code from the last command invocation.
    /// </summary>
    public int ExitCode { get; private set; }

    /// <summary>
    /// Invokes a command with the specified arguments.
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task InvokeAsync(Command command, params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            Console.SetOut(_standardOutput);
            Console.SetError(_standardError);

            ExitCode = await command.Parse(args).InvokeAsync().ConfigureAwait(false);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>
    /// Invokes a root command with the specified arguments.
    /// </summary>
    /// <param name="rootCommand">The root command to invoke.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task InvokeAsync(RootCommand rootCommand, params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            Console.SetOut(_standardOutput);
            Console.SetError(_standardError);

            ExitCode = await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>
    /// Resets the captured output and exit code.
    /// </summary>
    public void Reset()
    {
        _standardOutput.GetStringBuilder().Clear();
        _standardError.GetStringBuilder().Clear();
        ExitCode = 0;
    }
}
