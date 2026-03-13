namespace Anchor.Domain;

public sealed record ConflictExplanation
{
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
}

public sealed record DoctorIssue
{
    public ProblemSeverity Severity { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

public sealed record DoctorReport
{
    public string RepositoryRoot { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public bool IsHealthy { get; init; }
    public IReadOnlyList<DoctorIssue> Issues { get; init; } = Array.Empty<DoctorIssue>();
    public RepoState RepoState { get; init; } = new();
}

public sealed record CommandExplanation
{
    public string CommandText { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string HeadImpact { get; init; } = string.Empty;
    public string IndexImpact { get; init; } = string.Empty;
    public string WorkingTreeImpact { get; init; } = string.Empty;
    public string BranchImpact { get; init; } = string.Empty;
    public RiskLevel RiskLevel { get; init; }
    public string UndoGuidance { get; init; } = string.Empty;
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
    public bool UsedAI { get; init; }
}
