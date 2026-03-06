using System.Diagnostics;
using System.Text;

namespace Oras.Tests.Helpers;

/// <summary>
/// Helper for executing the oras CLI as a process and capturing output.
/// Used for integration testing the compiled CLI binary.
/// </summary>
public sealed class CliRunner
{
    private readonly string _cliPath;

    /// <summary>
    /// Initializes a new instance of the CliRunner.
    /// </summary>
    /// <param name="cliPath">Path to the oras CLI executable. If null, attempts to find in PATH.</param>
    public CliRunner(string? cliPath = null)
    {
        _cliPath = cliPath ?? FindCliExecutable();
    }

    /// <summary>
    /// Gets the captured standard output from the last execution.
    /// </summary>
    public string StandardOutput { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the captured standard error from the last execution.
    /// </summary>
    public string StandardError { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the exit code from the last execution.
    /// </summary>
    public int ExitCode { get; private set; }

    /// <summary>
    /// Executes the oras CLI with the specified arguments.
    /// </summary>
    /// <param name="args">Command-line arguments to pass to oras.</param>
    /// <param name="workingDirectory">Working directory for the process. If null, uses current directory.</param>
    /// <param name="environmentVariables">Additional environment variables to set.</param>
    /// <param name="timeoutSeconds">Timeout in seconds. Default is 30.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task<CliResult> ExecuteAsync(
        string args,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        int timeoutSeconds = 30)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _cliPath,
            Arguments = args,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (environmentVariables != null)
        {
            foreach (var kvp in environmentVariables)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds)).ConfigureAwait(false);

        if (!completed)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort
            }

            throw new TimeoutException($"CLI execution timed out after {timeoutSeconds} seconds.");
        }

        StandardOutput = outputBuilder.ToString().TrimEnd();
        StandardError = errorBuilder.ToString().TrimEnd();
        ExitCode = process.ExitCode;

        return new CliResult
        {
            ExitCode = ExitCode,
            StandardOutput = StandardOutput,
            StandardError = StandardError
        };
    }

    /// <summary>
    /// Executes the oras CLI with the specified arguments as an array.
    /// </summary>
    /// <param name="args">Command-line arguments to pass to oras.</param>
    /// <param name="workingDirectory">Working directory for the process. If null, uses current directory.</param>
    /// <param name="environmentVariables">Additional environment variables to set.</param>
    /// <param name="timeoutSeconds">Timeout in seconds. Default is 30.</param>
    /// <returns>Task representing the async operation.</returns>
    public Task<CliResult> ExecuteAsync(
        string[] args,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        int timeoutSeconds = 30)
    {
        var argsString = string.Join(" ", args.Select(EscapeArgument));
        return ExecuteAsync(argsString, workingDirectory, environmentVariables, timeoutSeconds);
    }

    private static string FindCliExecutable()
    {
        // Try to find the compiled CLI binary in the build output directory
        var assemblyLocation = typeof(CliRunner).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        
        // Navigate up to find the CLI project output
        var solutionRoot = FindSolutionRoot(assemblyDir);
        if (solutionRoot != null)
        {
            var cliProjectPath = Path.Combine(solutionRoot, "src", "Oras.Cli", "bin");
            if (Directory.Exists(cliProjectPath))
            {
                var exeName = OperatingSystem.IsWindows() ? "oras.exe" : "oras";
                var cliExe = Directory.GetFiles(cliProjectPath, exeName, SearchOption.AllDirectories)
                    .FirstOrDefault();
                
                if (cliExe != null)
                {
                    return cliExe;
                }
            }
        }

        // Fallback to PATH
        var exeFileName = OperatingSystem.IsWindows() ? "oras.exe" : "oras";
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        
        foreach (var dir in pathDirs)
        {
            var fullPath = Path.Combine(dir, exeFileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException(
            $"Could not find oras CLI executable. Searched in build output and PATH. " +
            $"Please build the CLI project first or provide the path explicitly.");
    }

    private static string? FindSolutionRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current != null)
        {
            if (current.GetFiles("*.slnx").Any() || current.GetFiles("*.sln").Any())
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        return null;
    }

    private static string EscapeArgument(string arg)
    {
        if (!arg.Contains(' ') && !arg.Contains('"'))
        {
            return arg;
        }

        return $"\"{arg.Replace("\"", "\\\"")}\"";
    }
}

/// <summary>
/// Represents the result of a CLI execution.
/// </summary>
public sealed class CliResult
{
    /// <summary>
    /// Gets the exit code.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// Gets the standard output.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// Gets the standard error.
    /// </summary>
    public required string StandardError { get; init; }

    /// <summary>
    /// Gets a value indicating whether the execution was successful (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;
}
