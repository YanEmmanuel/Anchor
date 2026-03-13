using Anchor.Domain;
using System.Text.Json.Serialization;

namespace Anchor.Application.Configuration;

public sealed class AnchorOptions
{
    public string Language { get; set; } = "auto";
    public string GitExecutablePath { get; set; } = "git";
    public CommitLanguageMode CommitLanguageMode { get; set; } = CommitLanguageMode.User;
    public string? CustomCommitLanguage { get; set; }
    public AIOptions AI { get; set; } = new();
    public SafetyOptions Safety { get; set; } = new();
}

public sealed class AIOptions
{
    public AiProviderType DefaultProvider { get; set; } = AiProviderType.Ollama;
    public int TimeoutSeconds { get; set; } = 45;
    public int MaxPromptDiffLines { get; set; } = 500;
    public LocalModelOptions Ollama { get; set; } = new();

    [JsonPropertyName("openaiCompatible")]
    public ExternalModelOptions OpenAICompatible { get; set; } = new();

    [JsonPropertyName("anthropicCompatible")]
    public ExternalModelOptions AnthropicCompatible { get; set; } = new();
}

public sealed class LocalModelOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5:14b-instruct";
}

public sealed class ExternalModelOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

public sealed class SafetyOptions
{
    public bool AutoSnapshotBeforeDangerousCommands { get; set; } = true;
    public string ConfirmationLevel { get; set; } = "strict";
}
