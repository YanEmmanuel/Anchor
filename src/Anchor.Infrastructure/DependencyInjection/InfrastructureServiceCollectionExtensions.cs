using Anchor.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Anchor.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorInfrastructureServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IAppPathProvider, AppPathProvider>();
        services.TryAddSingleton<ISessionState, SessionState>();
        services.TryAddSingleton<IEnvironmentReader, EnvironmentReader>();
        services.TryAddSingleton<IClipboardService, ClipboardService>();
        services.TryAddSingleton<ConfigurationBootstrapper>();

        return services;
    }
}
