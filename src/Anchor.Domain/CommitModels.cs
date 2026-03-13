namespace Anchor.Domain;

public sealed record CommitGenerationRequest
{
    public bool UseStagedChanges { get; init; } = true;
    public bool IncludeAllChanges { get; init; }
    public bool IncludeBody { get; init; } = true;
    public string? ProviderOverride { get; init; }
    public string? ModelOverride { get; init; }
    public string ResponseLanguage { get; init; } = "en";
    public bool CommitAfterAccept { get; init; }
    public bool CopyToClipboard { get; init; }
    public string? RepositoryRoot { get; init; }
}

public sealed record CommitIntentAnalysis
{
    public string InferredType { get; init; } = "chore";
    public string? InferredScope { get; init; }
    public bool MixedConcerns { get; init; }
    public int ChangedFileCount { get; init; }
    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ConcernGroups { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedModules { get; init; } = Array.Empty<string>();
}

public sealed record CommitSuggestion
{
    public string Type { get; init; } = "chore";
    public string? Scope { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Body { get; init; }
    public bool IsBreakingChange { get; init; }
    public string? BreakingChangeDescription { get; init; }
    public int Confidence { get; init; }
    public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ConcernGroups { get; init; } = Array.Empty<string>();

    public string Header =>
        string.IsNullOrWhiteSpace(Scope)
            ? $"{Type}: {Title}"
            : $"{Type}({Scope}): {Title}";

    public string ToCommitMessage()
    {
        var sections = new List<string> { Header };

        if (!string.IsNullOrWhiteSpace(Body))
        {
            sections.Add(Body.Trim());
        }

        if (IsBreakingChange && !string.IsNullOrWhiteSpace(BreakingChangeDescription))
        {
            sections.Add($"BREAKING CHANGE: {BreakingChangeDescription.Trim()}");
        }

        return string.Join(Environment.NewLine + Environment.NewLine, sections);
    }
}

public sealed record CommitGenerationResult
{
    public CommitSuggestion PrimarySuggestion { get; init; } = new();
    public IReadOnlyList<CommitSuggestion> Alternatives { get; init; } = Array.Empty<CommitSuggestion>();
    public CommitIntentAnalysis IntentAnalysis { get; init; } = new();
    public bool UsedAI { get; init; }
    public string ProviderName { get; init; } = "disabled";
    public string Model { get; init; } = "deterministic";
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
