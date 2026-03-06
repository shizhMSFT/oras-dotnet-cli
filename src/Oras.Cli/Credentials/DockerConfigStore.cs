using System.Text;
using System.Text.Json;

namespace Oras.Credentials;

/// <summary>
/// Docker config.json credential store.
/// </summary>
internal class DockerConfigStore
{
    private readonly string _configPath;

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
            var json = await File.ReadAllTextAsync(_configPath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize(json, CredentialJsonContext.Default.DockerConfig) ?? new DockerConfig();
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

        var json = JsonSerializer.Serialize(config, CredentialJsonContext.Default.DockerConfig);
        await File.WriteAllTextAsync(_configPath, json, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Aggregates all known registries from the Docker config by combining:
    /// <list type="bullet">
    ///   <item><c>auths</c> keys (direct auth entries)</item>
    ///   <item><c>credHelpers</c> keys (per-registry credential helper entries)</item>
    ///   <item>Entries returned by the global <c>credsStore</c> helper's <c>list</c> action</item>
    /// </list>
    /// Returns a deduplicated list of registry server addresses.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListRegistriesAsync(
        CancellationToken cancellationToken = default)
    {
        var config = await LoadAsync(cancellationToken).ConfigureAwait(false);
        var registries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Direct auth entries
        foreach (var key in config.Auths.Keys)
        {
            registries.Add(key);
        }

        // 2. Per-registry credential helper entries
        if (config.CredHelpers is not null)
        {
            foreach (var key in config.CredHelpers.Keys)
            {
                registries.Add(key);
            }
        }

        // 3. Global credential store — enumerate all stored credentials
        if (!string.IsNullOrEmpty(config.CredsStore))
        {
            var helper = new NativeCredentialHelper(config.CredsStore);
            var stored = await helper.ListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var key in stored.Keys)
            {
                registries.Add(key);
            }
        }

        return registries.ToList().AsReadOnly();
    }

    public async Task<(string? Username, string? Password)?> GetCredentialsAsync(
        string serverAddress,
        CancellationToken cancellationToken = default)
    {
        var config = await LoadAsync(cancellationToken).ConfigureAwait(false);

        // Check if there's a specific credential helper for this registry
        if (config.CredHelpers?.TryGetValue(serverAddress, out var helper) == true)
        {
            return await GetCredentialsFromHelperAsync(helper, serverAddress, cancellationToken).ConfigureAwait(false);
        }

        // Check if there's a global credential store
        if (!string.IsNullOrEmpty(config.CredsStore))
        {
            return await GetCredentialsFromHelperAsync(config.CredsStore, serverAddress, cancellationToken).ConfigureAwait(false);
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
        var config = await LoadAsync(cancellationToken).ConfigureAwait(false);

        // Check if there's a credential helper configured
        var helper = config.CredHelpers?.GetValueOrDefault(serverAddress) ?? config.CredsStore;
        if (!string.IsNullOrEmpty(helper))
        {
            await StoreCredentialsInHelperAsync(helper, serverAddress, username, password, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Store in auths section with base64 encoding
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        config.Auths[serverAddress] = new DockerAuth
        {
            Auth = authString
        };

        await SaveAsync(config, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveCredentialsAsync(
        string serverAddress,
        CancellationToken cancellationToken = default)
    {
        var config = await LoadAsync(cancellationToken).ConfigureAwait(false);

        // Check if there's a credential helper configured
        var helper = config.CredHelpers?.GetValueOrDefault(serverAddress) ?? config.CredsStore;
        if (!string.IsNullOrEmpty(helper))
        {
            await RemoveCredentialsFromHelperAsync(helper, serverAddress, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Remove from auths section
        config.Auths.Remove(serverAddress);
        await SaveAsync(config, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(string Username, string Password)?> GetCredentialsFromHelperAsync(
        string helper,
        string serverAddress,
        CancellationToken cancellationToken)
    {
        var credHelper = new NativeCredentialHelper(helper);
        return await credHelper.GetAsync(serverAddress, cancellationToken).ConfigureAwait(false);
    }

    private async Task StoreCredentialsInHelperAsync(
        string helper,
        string serverAddress,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var credHelper = new NativeCredentialHelper(helper);
        await credHelper.StoreAsync(serverAddress, username, password, cancellationToken).ConfigureAwait(false);
    }

    private async Task RemoveCredentialsFromHelperAsync(
        string helper,
        string serverAddress,
        CancellationToken cancellationToken)
    {
        var credHelper = new NativeCredentialHelper(helper);
        await credHelper.EraseAsync(serverAddress, cancellationToken).ConfigureAwait(false);
    }
}
