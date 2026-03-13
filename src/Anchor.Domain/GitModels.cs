namespace Anchor.Domain;

public sealed record LanguagePreference(string Value);

public sealed record GitCommand(string Executable, IReadOnlyList<string> Arguments, string DisplayText);

public sealed record GitCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool IsSuccess => ExitCode == 0;
}

public sealed record BranchInfo(
    string? CurrentBranch,
    string? UpstreamBranch,
    int AheadBy,
    int BehindBy,
    bool IsDetachedHead);

public sealed record RepoState
{
    public static RepoState Empty(string repositoryRoot) => new()
    {
        RepositoryRoot = repositoryRoot
    };

    public string RepositoryRoot { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public string HeadSha { get; init; } = string.Empty;
    public bool IsDetachedHead { get; init; }
    public bool HasStagedChanges { get; init; }
    public bool HasUnstagedChanges { get; init; }
    public bool HasUntrackedFiles { get; init; }
    public bool HasConflicts { get; init; }
    public int AheadBy { get; init; }
    public int BehindBy { get; init; }
    public IReadOnlyList<string> ChangedFiles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> UntrackedFiles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ConflictFiles { get; init; } = Array.Empty<string>();
}

public sealed record DiffContent
{
    public string RangeDescription { get; init; } = string.Empty;
    public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();
    public string PatchText { get; init; } = string.Empty;
    public int AddedLines { get; init; }
    public int RemovedLines { get; init; }
    public bool IsTruncated { get; init; }
}

public sealed record GitCommitSummary
{
    public string Sha { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public DateTimeOffset Date { get; init; }
    public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();
}

public sealed record GitRepoContext
{
    public string RepositoryRoot { get; init; } = string.Empty;
    public string? BranchName { get; init; }
    public string HeadSha { get; init; } = string.Empty;
    public RepoState State { get; init; } = new();
    public IReadOnlyList<GitCommitSummary> RecentCommits { get; init; } = Array.Empty<GitCommitSummary>();
}

public sealed record GitCommandContext
{
    public string RawCommand { get; init; } = string.Empty;
    public IReadOnlyList<string> Tokens { get; init; } = Array.Empty<string>();
    public string RepositoryRoot { get; init; } = string.Empty;
    public RepoState RepoState { get; init; } = new();
}
