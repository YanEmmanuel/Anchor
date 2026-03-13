namespace Anchor.Domain;

public sealed record UserLanguageContext
{
    public string LanguageTag { get; init; } = "en";
    public string DisplayName { get; init; } = "English";
    public LanguageDetectionSource Source { get; init; } = LanguageDetectionSource.Fallback;
}

public sealed record PullRequestSummary
{
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Changes { get; init; } = Array.Empty<string>();
    public string Motivation { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string? Testing { get; init; }
}

public sealed record PullRequestSummaryResult
{
    public PullRequestSummary Summary { get; init; } = new();
    public bool UsedAI { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed record WorkSummaryResult
{
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Risks { get; init; } = Array.Empty<string>();
    public bool UsedAI { get; init; }
}

public sealed record WhyFileResult
{
    public string FilePath { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> SupportingCommits { get; init; } = Array.Empty<string>();
    public bool UsedAI { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed record BranchNameSuggestion
{
    public string Name { get; init; } = string.Empty;
    public string Prefix { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int Confidence { get; init; }
    public IReadOnlyList<string> Alternatives { get; init; } = Array.Empty<string>();
    public bool UsedAI { get; init; }
}
