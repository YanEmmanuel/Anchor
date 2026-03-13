using Anchor.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Diagnostics.DependencyInjection;

public static class DiagnosticsServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorDiagnosticsServices(this IServiceCollection services)
    {
        services.AddSingleton<DetachedHeadDetector>();
        services.AddSingleton<InterruptedRebaseDetector>();
        services.AddSingleton<PendingMergeDetector>();
        services.AddSingleton<UntrackedArtifactsDetector>();
        services.AddSingleton<BranchDivergenceDetector>();
        services.AddSingleton<DirtyWorkingTreeDetector>();
        services.AddSingleton<ConflictDetector>();
        services.AddSingleton<IRepositoryDoctor, RepositoryDoctor>();
        services.AddSingleton<IRiskAnalyzer, RiskAnalyzer>();
        return services;
    }
}
