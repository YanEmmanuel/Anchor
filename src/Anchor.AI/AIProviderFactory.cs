using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.AI;

public sealed class AIProviderFactory : IAIProviderFactory
{
    private readonly IOptions<AnchorOptions> _options;
    private readonly OllamaAIProvider _ollama;
    private readonly OpenAICompatibleProvider _openAiCompatible;
    private readonly AnthropicCompatibleProvider _anthropicCompatible;
    private readonly DisabledAIProvider _disabled;

    public AIProviderFactory(
        IOptions<AnchorOptions> options,
        OllamaAIProvider ollama,
        OpenAICompatibleProvider openAiCompatible,
        AnthropicCompatibleProvider anthropicCompatible,
        DisabledAIProvider disabled)
    {
        _options = options;
        _ollama = ollama;
        _openAiCompatible = openAiCompatible;
        _anthropicCompatible = anthropicCompatible;
        _disabled = disabled;
    }

    public IAIProvider Create(string? providerOverride = null)
    {
        var provider = ParseProvider(providerOverride) ?? _options.Value.AI.DefaultProvider;
        return provider switch
        {
            AiProviderType.Ollama => _ollama,
            AiProviderType.OpenAiCompatible => _openAiCompatible,
            AiProviderType.AnthropicCompatible => _anthropicCompatible,
            _ => _disabled
        };
    }

    private static AiProviderType? ParseProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return null;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "ollama" => AiProviderType.Ollama,
            "openai" or "openai-compatible" => AiProviderType.OpenAiCompatible,
            "anthropic" or "anthropic-compatible" => AiProviderType.AnthropicCompatible,
            "disabled" or "off" => AiProviderType.Disabled,
            _ => null
        };
    }
}
