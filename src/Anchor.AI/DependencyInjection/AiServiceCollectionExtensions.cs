using Anchor.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.AI.DependencyInjection;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorAiServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<ICommitPromptBuilder, CommitPromptBuilder>();
        services.AddSingleton<IExplainPromptBuilder, ExplainPromptBuilder>();
        services.AddSingleton<IConflictPromptBuilder, ConflictPromptBuilder>();
        services.AddSingleton<ISummaryPromptBuilder, SummaryPromptBuilder>();
        services.AddSingleton<OllamaAIProvider>();
        services.AddSingleton<OpenAICompatibleProvider>();
        services.AddSingleton<AnthropicCompatibleProvider>();
        services.AddSingleton<DisabledAIProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        return services;
    }
}
