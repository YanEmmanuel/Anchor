using Anchor.Application.Services;
using Anchor.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<CommitIntentAnalyzer>();
        services.AddSingleton<CommitMessageFallbackGenerator>();
        services.AddSingleton<CommandExplainer>();
        services.AddSingleton<SummaryFallbackComposer>();
        services.AddSingleton<DetectUserLanguageUseCase>();
        services.AddSingleton<CommitAiUseCase>();
        services.AddSingleton<GenerateCommitMessageUseCase>();
        services.AddSingleton<ExplainCommandUseCase>();
        services.AddSingleton<DoctorUseCase>();
        services.AddSingleton<PreviewRiskyCommandUseCase>();
        services.AddSingleton<ExecuteGitCommandUseCase>();
        services.AddSingleton<SnapshotUseCase>();
        services.AddSingleton<RecoverUseCase>();
        services.AddSingleton<PrSummaryUseCase>();
        services.AddSingleton<SummarizeUseCase>();
        services.AddSingleton<WhyFileUseCase>();
        services.AddSingleton<BranchAiUseCase>();
        return services;
    }
}
