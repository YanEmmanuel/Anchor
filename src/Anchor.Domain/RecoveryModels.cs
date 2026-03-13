namespace Anchor.Domain;

public sealed record Snapshot
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string RepositoryRoot { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public string HeadSha { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public string? ReferenceName { get; init; }
    public string? StashCommit { get; init; }
    public bool IncludesWorkingTree { get; init; }
    public string? Notes { get; init; }
}

public sealed record SnapshotMetadata
{
    public string Id { get; init; } = string.Empty;
    public string RepositoryId { get; init; } = string.Empty;
    public string RepositoryRoot { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public string HeadSha { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public string? RefName { get; init; }
    public string? StashCommit { get; init; }
    public bool IncludesWorkingTree { get; init; }
    public string? Notes { get; init; }

    public Snapshot ToSnapshot() => new()
    {
        Id = Id,
        Description = Description,
        RepositoryRoot = RepositoryRoot,
        CreatedAt = CreatedAt,
        HeadSha = HeadSha,
        BranchName = BranchName,
        ReferenceName = RefName,
        StashCommit = StashCommit,
        IncludesWorkingTree = IncludesWorkingTree,
        Notes = Notes
    };
}

public sealed record RecoveryPoint
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public string RepositoryRoot { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public string HeadSha { get; init; } = string.Empty;
    public bool HasWorkingTreeSnapshot { get; init; }
    public string? Notes { get; init; }
}

public sealed record CommandExecutionPlan
{
    public GitCommand Command { get; init; } = new("git", Array.Empty<string>(), string.Empty);
    public RiskLevel RiskLevel { get; init; }
    public bool RequiresConfirmation { get; init; }
    public bool SnapshotCreated { get; init; }
    public string? SnapshotId { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Alternatives { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PotentiallyAffectedFiles { get; init; } = Array.Empty<string>();
}

public sealed record CommandRiskAnalysis
{
    public RiskLevel RiskLevel { get; init; }
    public bool RequiresConfirmation { get; init; }
    public bool ShouldCreateSnapshot { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PotentiallyAffectedFiles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> LostCommits { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Alternatives { get; init; } = Array.Empty<string>();
}
