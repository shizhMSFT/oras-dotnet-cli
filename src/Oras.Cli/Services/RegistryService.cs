using OrasProject.Oras.Registry.Remote;

namespace Oras.Services;

/// <summary>
/// Registry service implementation using OrasProject.Oras library.
/// NOTE: This is a stub implementation. The actual OrasProject.Oras v0.5.0 API
/// needs to be properly integrated once the API surface is documented.
/// </summary>
internal class RegistryService : IRegistryService
{
    private readonly ICredentialService _credentialService;

    public RegistryService(ICredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    public async Task<Registry> CreateRegistryAsync(
        string registryHost,
        string? username = null,
        string? password = null,
        bool plainHttp = false,
        bool insecure = false,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false); // Suppress async warning

        // TODO: Implement actual Registry creation with proper OrasProject.Oras v0.5.0 API
        throw new NotImplementedException(
            "Registry creation needs OrasProject.Oras v0.5.0 API integration. " +
            "The actual constructor signature differs from expected.");
    }

    public async Task<Repository> CreateRepositoryAsync(
        string reference,
        string? username = null,
        string? password = null,
        bool plainHttp = false,
        bool insecure = false,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false); // Suppress async warning

        // TODO: Implement actual Repository creation with proper OrasProject.Oras v0.5.0 API
        throw new NotImplementedException(
            "Repository creation needs OrasProject.Oras v0.5.0 API integration. " +
            "The actual API surface differs from expected.");
    }
}
