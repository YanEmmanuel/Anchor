using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.AI;

public sealed class DisabledAIProvider : IAIProvider
{
    public AiProviderType ProviderType => AiProviderType.Disabled;

    public Task<ProviderHealthStatus> CheckHealthAsync(string? modelOverride, CancellationToken cancellationToken) =>
        Task.FromResult(new ProviderHealthStatus
        {
            ProviderType = ProviderType,
            IsAvailable = false,
            Model = modelOverride,
            Message = "AI is disabled."
        });

    public Task<AIResponse> GenerateAsync(AIRequestContext request, CancellationToken cancellationToken) =>
        Task.FromResult(new AIResponse
        {
            Success = false,
            ProviderName = "disabled",
            Model = request.ModelOverride ?? "disabled",
            ErrorMessage = "AI is disabled."
        });
}
