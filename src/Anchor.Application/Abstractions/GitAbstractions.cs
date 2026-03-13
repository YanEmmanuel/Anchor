using Anchor.Domain;

namespace Anchor.Application.Abstractions;

public interface IGitRepositoryLocator
{
    Task<string> LocateAsync(string? startPath, CancellationToken cancellationToken);
}

public interface IGitStatusReader
{
    Task<RepoState> ReadAsync(string repositoryRoot, CancellationToken cancellationToken);
}

public interface IGitDiffReader
{
    Task<DiffContent> ReadStagedDiffAsync(string repositoryRoot, int maxLines, CancellationToken cancellationToken);
    Task<DiffContent> ReadWorkingTreeDiffAsync(string repositoryRoot, int maxLines, CancellationToken cancellationToken);
    Task<DiffContent> ReadAllChangesDiffAsync(string repositoryRoot, int maxLines, CancellationToken cancellationToken);
    Task<DiffContent> ReadComparisonDiffAsync(string repositoryRoot, string baseRef, string headRef, int maxLines, CancellationToken cancellationToken);
}

public interface IGitLogReader
{
    Task<IReadOnlyList<GitCommitSummary>> ReadRecentCommitsAsync(string repositoryRoot, int count, CancellationToken cancellationToken);
    Task<IReadOnlyList<GitCommitSummary>> ReadFileHistoryAsync(string repositoryRoot, string filePath, int count, CancellationToken cancellationToken);
}

public interface IGitBranchReader
{
    Task<BranchInfo> ReadAsync(string repositoryRoot, CancellationToken cancellationToken);
    Task<string> ResolveBaseBranchAsync(string repositoryRoot, CancellationToken cancellationToken);
}

public interface IGitCommandExecutor
{
    Task<GitCommandResult> ExecuteAsync(string repositoryRoot, IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}

public interface IGitConflictReader
{
    Task<IReadOnlyList<string>> ReadConflictedFilesAsync(string repositoryRoot, CancellationToken cancellationToken);
}

public interface IGitWorktreeAnalyzer
{
    Task<GitRepoContext> AnalyzeAsync(string repositoryRoot, CancellationToken cancellationToken);
}

public interface IGitReflogReader
{
    Task<IReadOnlyList<RecoveryPoint>> ReadAsync(string repositoryRoot, int count, CancellationToken cancellationToken);
}
