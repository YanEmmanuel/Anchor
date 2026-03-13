namespace Anchor.Domain;

public enum RiskLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum AiProviderType
{
    Ollama = 0,
    OpenAiCompatible = 1,
    AnthropicCompatible = 2,
    Disabled = 3
}

public enum CommitLanguageMode
{
    User = 0,
    English = 1,
    Custom = 2
}

public enum LanguageDetectionSource
{
    Configuration = 0,
    CommandLine = 1,
    Environment = 2,
    CurrentUiCulture = 3,
    CurrentCulture = 4,
    LangEnvironment = 5,
    Fallback = 6
}

public enum ProblemSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}
