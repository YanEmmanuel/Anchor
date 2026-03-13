using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Git;

public sealed class GitWorktreeAnalyzer : IGitWorktreeAnalyzer
{
    private readonly IGitStatusReader _statusReader;
    private readonly IGitLogReader _logReader;

    public GitWorktreeAnalyzer(IGitStatusReader statusReader, IGitLogReader logReader)
    {
        _statusReader = statusReader;
        _logReader = logReader;
    }

    public async Task<GitRepoContext> AnalyzeAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var stateTask = _statusReader.ReadAsync(repositoryRoot, cancellationToken);
        var recentCommitsTask = _logReader.ReadRecentCommitsAsync(repositoryRoot, 10, cancellationToken);

        await Task.WhenAll(stateTask, recentCommitsTask);

        var state = await stateTask;
        return new GitRepoContext
        {
            RepositoryRoot = repositoryRoot,
            BranchName = state.BranchName,
            HeadSha = state.HeadSha,
            State = state,
            RecentCommits = await recentCommitsTask
        };
    }
}
