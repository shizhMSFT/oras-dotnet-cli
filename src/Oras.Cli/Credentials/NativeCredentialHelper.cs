using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace Oras.Credentials;

/// <summary>
/// Native credential helper implementation following docker-credential-helpers protocol.
/// Protocol spec: https://github.com/docker/docker-credential-helpers
/// </summary>
internal class NativeCredentialHelper
{
    private readonly string _helperName;

    public NativeCredentialHelper(string helperName)
    {
        _helperName = helperName.StartsWith("docker-credential-")
            ? helperName
            : $"docker-credential-{helperName}";
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    public async Task<(string Username, string Password)?> GetAsync(
        string serverAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunHelperAsync("get", serverAddress, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(output))
            {
                return null;
            }

            var response = JsonSerializer.Deserialize<CredentialHelperResponse>(output);
            if (response?.Username != null)
            {
                return (response.Username, response.Secret ?? string.Empty);
            }
        }
        catch
        {
            // Helper failed or not found
        }

        return null;
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public async Task StoreAsync(
        string serverAddress,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var input = JsonSerializer.Serialize(new CredentialHelperInput
        {
            ServerURL = serverAddress,
            Username = username,
            Secret = password
        });

        await RunHelperAsync("store", input, cancellationToken).ConfigureAwait(false);
    }

    public async Task EraseAsync(
        string serverAddress,
        CancellationToken cancellationToken = default)
    {
        await RunHelperAsync("erase", serverAddress, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> RunHelperAsync(
        string action,
        string input,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _helperName,
            Arguments = action,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.StandardInput.WriteAsync(input).ConfigureAwait(false);
        await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
        process.StandardInput.Close();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var error = errorBuilder.ToString().Trim();
            throw new InvalidOperationException(
                $"Credential helper '{_helperName}' failed: {error}");
        }

        return outputBuilder.ToString().Trim();
    }

    private class CredentialHelperInput
    {
        [System.Text.Json.Serialization.JsonPropertyName("ServerURL")]
        public string ServerURL { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("Username")]
        public string Username { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("Secret")]
        public string Secret { get; set; } = string.Empty;
    }

    private class CredentialHelperResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("Username")]
        public string? Username { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("Secret")]
        public string? Secret { get; set; }
    }
}
