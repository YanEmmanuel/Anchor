using Anchor.Application.Abstractions;
using Anchor.Domain;
using Microsoft.Extensions.Logging;

namespace Anchor.Recovery;

public sealed class RecoveryService : IRecoveryService
{
    private readonly SnapshotStore _snapshotStore;
    private readonly IGitCommandExecutor _gitCommandExecutor;
    private readonly IGitStatusReader _statusReader;
    private readonly ILogger<RecoveryService> _logger;

    public RecoveryService(
        SnapshotStore snapshotStore,
        IGitCommandExecutor gitCommandExecutor,
        IGitStatusReader statusReader,
        ILogger<RecoveryService> logger)
    {
        _snapshotStore = snapshotStore;
        _gitCommandExecutor = gitCommandExecutor;
        _statusReader = statusReader;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecoveryPoint>> ListAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var snapshots = await _snapshotStore.ListAsync(repositoryRoot, cancellationToken);
        return snapshots
            .Select(static snapshot => new RecoveryPoint
            {
                Id = snapshot.Id,
                Description = snapshot.Description,
                CreatedAt = snapshot.CreatedAt,
                RepositoryRoot = snapshot.RepositoryRoot,
                BranchName = snapshot.BranchName,
                HeadSha = snapshot.HeadSha,
                HasWorkingTreeSnapshot = snapshot.IncludesWorkingTree,
                Notes = snapshot.Notes
            })
            .ToArray();
    }

    public async Task<GitCommandResult> RestoreAsync(string repositoryRoot, string snapshotId, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotStore.GetAsync(repositoryRoot, snapshotId, cancellationToken)
                       ?? throw new InvalidOperationException($"Snapshot '{snapshotId}' was not found.");

        var currentState = await _statusReader.ReadAsync(repositoryRoot, cancellationToken);
        if (currentState.HasStagedChanges || currentState.HasUnstagedChanges || currentState.HasUntrackedFiles)
        {
            throw new InvalidOperationException("Working tree must be clean before restoring a snapshot.");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.BranchName) && !string.Equals(currentState.BranchName, snapshot.BranchName, StringComparison.OrdinalIgnoreCase))
        {
            var switchResult = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, ["switch", snapshot.BranchName], cancellationToken);
            if (!switchResult.IsSuccess)
            {
                throw new InvalidOperationException(switchResult.StandardError);
            }
        }

        var resetResult = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, ["reset", "--hard", snapshot.HeadSha], cancellationToken);
        if (!resetResult.IsSuccess)
        {
            throw new InvalidOperationException(resetResult.StandardError);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.StashCommit))
        {
            var stashApplyResult = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, ["stash", "apply", snapshot.StashCommit], cancellationToken);
            if (!stashApplyResult.IsSuccess)
            {
                _logger.LogWarning("Snapshot {SnapshotId} restored HEAD but failed to re-apply stash {StashCommit}: {Error}", snapshotId, snapshot.StashCommit, stashApplyResult.StandardError);
                return new GitCommandResult(stashApplyResult.ExitCode, resetResult.StandardOutput, stashApplyResult.StandardError);
            }
        }

        _logger.LogInformation("Restored snapshot {SnapshotId} for {RepositoryRoot}", snapshotId, repositoryRoot);
        return resetResult;
    }
}
