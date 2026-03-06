using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace Oras;

/// <summary>
/// Global error handler for ORAS CLI commands.
/// </summary>
internal static class ErrorHandler
{
    /// <summary>
    /// Wrap a command action with error handling.
    /// </summary>
    [RequiresDynamicCode("Calls Spectre.Console.AnsiConsole.WriteException(Exception, ExceptionFormats)")]
    public static async Task<int> HandleAsync(Func<Task<int>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (OrasUsageException ex)
        {
            WriteError(ex.Message, ex.Recommendation);
            return 2; // Usage error
        }
        catch (OrasException ex)
        {
            WriteError(ex.Message, ex.Recommendation);
            return 1; // General error
        }
        catch (HttpRequestException ex)
        {
            WriteError(
                $"Network error: {ex.Message}",
                "Check your network connection and registry address. Ensure the registry is accessible.");
            return 1;
        }
        catch (TaskCanceledException)
        {
            WriteError(
                "Operation timed out or was cancelled",
                "Try again or check your network connection.");
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            WriteError(
                $"Access denied: {ex.Message}",
                "Check file permissions or run 'oras login' to authenticate.");
            return 1;
        }
        catch (Exception ex)
        {
            WriteError(
                $"Unexpected error: {ex.Message}",
                "This may be a bug. Please report it at https://github.com/oras-project/oras-dotnet-cli/issues");

            // In debug mode, show stack trace
            if (Environment.GetEnvironmentVariable("ORAS_DEBUG") == "1")
            {
                AnsiConsole.WriteException(ex);
            }

            return 1;
        }
    }

    private static void WriteError(string message, string? recommendation)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {message}");

        if (!string.IsNullOrEmpty(recommendation))
        {
            AnsiConsole.MarkupLine($"[yellow]Recommendation:[/] {recommendation}");
        }
    }
}
