namespace Anchor.Domain;

public sealed record AIRequestContext
{
    public string TaskName { get; init; } = string.Empty;
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public string Language { get; init; } = "en";
    public string? ProviderOverride { get; init; }
    public string? ModelOverride { get; init; }
    public double Temperature { get; init; } = 0.2d;
    public int MaxTokens { get; init; } = 1200;
    public TimeSpan? Timeout { get; init; }
}

public sealed record AIResponse
{
    public bool Success { get; init; }
    public string Content { get; init; } = string.Empty;
    public string ProviderName { get; init; } = "disabled";
    public string Model { get; init; } = "deterministic";
    public string? RawResponse { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record ProviderHealthStatus
{
    public AiProviderType ProviderType { get; init; }
    public bool IsAvailable { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Model { get; init; }
}
