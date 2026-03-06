using System.Text;
using System.Text.Json;

namespace Oras.Credentials;

/// <summary>
/// Docker config.json credential store.
/// </summary>
public class DockerConfigStore
{
    private readonly string _configPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DockerConfigStore(string? configPath = null)
    {
        _configPath = configPath ?? GetDefaultConfigPath();
    }

    private static string GetDefaultConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".docker", "config.json");
    }

    public async Task<DockerConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configPath))
        {
            return new DockerConfig();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configPath, cancellationToken);
            return JsonSerializer.Deserialize<DockerConfig>(json, JsonOptions) ?? new DockerConfig();
        }
        catch
        {
            return new DockerConfig();
        }
    }

    public async Task SaveAsync(DockerConfig config, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(_configPath, json, cancellationToken);
    }

    public async Task<(string? Username, string? Password)?> GetCredentialsAsync(
        string serverAddress,
        CancellationToken cancellationToken = default)
    {
        var config = await LoadAsync(cancellationToken);

        // Check if there's a specific credential helper for this registry
        if (config.CredHelpers?.TryGetValue(serverAddress, out var helper) == true)
        {
            return await GetCredentialsFromHelperAsync(helper, serverAddress, cancellationToken);
        }

        // Check if there's a global credential store
        if (!string.IsNullOrEmpty(config.CredsStore))
        {
            return await GetCredentialsFromHelperAsync(config.CredsStore, serverAddress, cancellationToken);
        }

        // Fall back to auths section
        if (config.Auths.TryGetValue(serverAddress, out var auth))
        {
            if (!string.IsNullOrEmpty(auth.Username))
            {
                return (auth.Username, auth.Password);
            }

            if (!string.IsNullOrEmpty(auth.Auth))
            {
                // Decode base64 auth string
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Auth));
                    var parts = decoded.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        return (parts[0], parts[1]);
                    }
                }
                catch
                {
                    // Invalid base64, ignore
                }
            }
        }

        return null;
    }

    public async Task StoreCredentialsAsync(
        string serverAddress,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var config = await LoadAsync(cancellationToken);

        // Check if there's a credential helper configured
        var helper = config.CredHelpers?.GetValueOrDefault(serverAddress) ?? config.CredsStore;
        if (!string.IsNullOrEmpty(helper))
        {
            await StoreCredentialsInHelperAsync(helper, serverAddress, username, password, cancellationToken);
            return;
        }

        // Store in auths section with base64 encoding
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        config.Auths[serverAddress] = new DockerAuth
        {
            Auth = authString
        };

        await SaveAsync(config, cancellationToken);
    }

    public async Task RemoveCredentialsAsync(
        string serverAddress,
        CancellationToken cancellationToken = default)
    {
        var config = await LoadAsync(cancellationToken);

        // Check if there's a credential helper configured
        var helper = config.CredHelpers?.GetValueOrDefault(serverAddress) ?? config.CredsStore;
        if (!string.IsNullOrEmpty(helper))
        {
            await RemoveCredentialsFromHelperAsync(helper, serverAddress, cancellationToken);
            return;
        }

        // Remove from auths section
        config.Auths.Remove(serverAddress);
        await SaveAsync(config, cancellationToken);
    }

    private async Task<(string Username, string Password)?> GetCredentialsFromHelperAsync(
        string helper,
        string serverAddress,
        CancellationToken cancellationToken)
    {
        var credHelper = new NativeCredentialHelper(helper);
        return await credHelper.GetAsync(serverAddress, cancellationToken);
    }

    private async Task StoreCredentialsInHelperAsync(
        string helper,
        string serverAddress,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var credHelper = new NativeCredentialHelper(helper);
        await credHelper.StoreAsync(serverAddress, username, password, cancellationToken);
    }

    private async Task RemoveCredentialsFromHelperAsync(
        string helper,
        string serverAddress,
        CancellationToken cancellationToken)
    {
        var credHelper = new NativeCredentialHelper(helper);
        await credHelper.EraseAsync(serverAddress, cancellationToken);
    }
}
