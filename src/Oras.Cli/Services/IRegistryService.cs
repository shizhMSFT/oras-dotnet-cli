using OrasProject.Oras.Registry.Remote;

namespace Oras.Services;

/// <summary>
/// Service for interacting with OCI registries.
/// </summary>
internal interface IRegistryService
{
    /// <summary>
    /// Create a registry client with the provided credentials.
    /// </summary>
    Task<Registry> CreateRegistryAsync(
        string registryHost,
        string? username = null,
        string? password = null,
        bool plainHttp = false,
        bool insecure = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a repository client for a specific registry/repository path.
    /// </summary>
    Task<Repository> CreateRepositoryAsync(
        string reference,
        string? username = null,
        string? password = null,
        bool plainHttp = false,
        bool insecure = false,
        CancellationToken cancellationToken = default);
}
