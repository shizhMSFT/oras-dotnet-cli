namespace Oras.Services;

/// <summary>
/// Service for managing registry credentials.
/// </summary>
public interface ICredentialService
{
    /// <summary>
    /// Validate credentials against a registry.
    /// </summary>
    Task<bool> ValidateCredentialsAsync(
        string registryHost,
        string username,
        string password,
        bool plainHttp = false,
        bool insecure = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Store credentials for a registry.
    /// </summary>
    Task StoreCredentialsAsync(
        string registryHost,
        string username,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove credentials for a registry.
    /// </summary>
    Task RemoveCredentialsAsync(
        string registryHost,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stored credentials for a registry.
    /// </summary>
    Task<(string? Username, string? Password)?> GetCredentialsAsync(
        string registryHost,
        CancellationToken cancellationToken = default);
}
