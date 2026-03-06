using Microsoft.Extensions.DependencyInjection;
using Oras.Credentials;

namespace Oras.Services;

/// <summary>
/// Extension methods for registering services with DI container.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add ORAS CLI services to the service collection.
    /// </summary>
    public static IServiceCollection AddOrasServices(this IServiceCollection services)
    {
        services.AddSingleton<ICredentialService, CredentialService>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IPushService, PushService>();
        services.AddSingleton<IPullService, PullService>();
        services.AddSingleton<DockerConfigStore>();

        return services;
    }
}
