using Anchor.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Recovery.DependencyInjection;

public static class RecoveryServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorRecoveryServices(this IServiceCollection services)
    {
        services.AddSingleton<SnapshotStore>();
        services.AddSingleton<ISnapshotService, SnapshotService>();
        services.AddSingleton<IRecoveryService, RecoveryService>();
        return services;
    }
}
