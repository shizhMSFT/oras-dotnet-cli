using Microsoft.Extensions.DependencyInjection;

namespace Oras.Services;

/// <summary>
/// Extension methods for registering services with DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add ORAS CLI services to the service collection.
    /// </summary>
    public static IServiceCollection AddOrasServices(this IServiceCollection services)
    {
        services.AddSingleton<ICredentialService, CredentialService>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddTransient<IPushService, PushService>();
        services.AddTransient<IPullService, PullService>();
        
        return services;
    }
}
