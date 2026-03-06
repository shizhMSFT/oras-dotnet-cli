namespace Oras.Tests.Helpers;

/// <summary>
/// In-memory credential store for testing login/logout functionality
/// without modifying the real Docker config file.
/// </summary>
public sealed class TestCredentialStore
{
    private readonly Dictionary<string, CredentialEntry> _credentials = new();

    /// <summary>
    /// Stores credentials for a registry.
    /// </summary>
    /// <param name="registry">The registry hostname.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password or token.</param>
    public void Store(string registry, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(registry))
        {
            throw new ArgumentException("Registry cannot be null or empty.", nameof(registry));
        }

        _credentials[registry] = new CredentialEntry(username, password);
    }

    /// <summary>
    /// Retrieves credentials for a registry.
    /// </summary>
    /// <param name="registry">The registry hostname.</param>
    /// <returns>The credential entry if found; otherwise, null.</returns>
    public CredentialEntry? Get(string registry)
    {
        if (string.IsNullOrWhiteSpace(registry))
        {
            throw new ArgumentException("Registry cannot be null or empty.", nameof(registry));
        }

        return _credentials.TryGetValue(registry, out var entry) ? entry : null;
    }

    /// <summary>
    /// Removes credentials for a registry.
    /// </summary>
    /// <param name="registry">The registry hostname.</param>
    /// <returns>True if credentials were removed; false if no credentials existed.</returns>
    public bool Remove(string registry)
    {
        if (string.IsNullOrWhiteSpace(registry))
        {
            throw new ArgumentException("Registry cannot be null or empty.", nameof(registry));
        }

        return _credentials.Remove(registry);
    }

    /// <summary>
    /// Checks if credentials exist for a registry.
    /// </summary>
    /// <param name="registry">The registry hostname.</param>
    /// <returns>True if credentials exist; otherwise, false.</returns>
    public bool Contains(string registry)
    {
        if (string.IsNullOrWhiteSpace(registry))
        {
            throw new ArgumentException("Registry cannot be null or empty.", nameof(registry));
        }

        return _credentials.ContainsKey(registry);
    }

    /// <summary>
    /// Clears all stored credentials.
    /// </summary>
    public void Clear()
    {
        _credentials.Clear();
    }

    /// <summary>
    /// Gets the number of stored credentials.
    /// </summary>
    public int Count => _credentials.Count;

    /// <summary>
    /// Represents a credential entry.
    /// </summary>
    public sealed record CredentialEntry(string Username, string Password);
}
