using Microsoft.Extensions.Caching.Memory;
using OrasProject.Oras.Registry;
using OrasProject.Oras.Registry.Remote;
using OrasProject.Oras.Registry.Remote.Auth;

namespace Oras.Services;

/// <summary>
/// Registry service implementation using OrasProject.Oras library.
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
        var client = await CreateClientAsync(registryHost, username, password, cancellationToken).ConfigureAwait(false);

        var options = new RepositoryOptions
        {
            Client = client,
            Reference = Reference.Parse(registryHost),
            PlainHttp = plainHttp
        };

        var registry = new Registry(options);
        return registry;
    }

    public async Task<Repository> CreateRepositoryAsync(
        string reference,
        string? username = null,
        string? password = null,
        bool plainHttp = false,
        bool insecure = false,
        CancellationToken cancellationToken = default)
    {
        var parsedRef = Reference.Parse(reference);
        var registryHost = parsedRef.Registry;

        var client = await CreateClientAsync(registryHost, username, password, cancellationToken).ConfigureAwait(false);

        var options = new RepositoryOptions
        {
            Client = client,
            Reference = parsedRef,
            PlainHttp = plainHttp
        };

        var repository = new Repository(options);
        return repository;
    }

    private async Task<IClient> CreateClientAsync(
        string registryHost,
        string? username,
        string? password,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            var credential = new Credential(username, password, "", "");
            var provider = new SingleRegistryCredentialProvider(registryHost, credential);
            var cache = new Cache(new MemoryCache(new MemoryCacheOptions()));
            return new Client(new HttpClient(), provider, cache);
        }

        var storedCreds = await _credentialService.GetCredentialsAsync(registryHost, cancellationToken).ConfigureAwait(false);
        if (storedCreds.HasValue && !string.IsNullOrEmpty(storedCreds.Value.Username))
        {
            var credential = new Credential(storedCreds.Value.Username, storedCreds.Value.Password ?? "", "", "");
            var provider = new SingleRegistryCredentialProvider(registryHost, credential);
            var cache = new Cache(new MemoryCache(new MemoryCacheOptions()));
            return new Client(new HttpClient(), provider, cache);
        }

        return new PlainClient(new HttpClient());
    }
}
