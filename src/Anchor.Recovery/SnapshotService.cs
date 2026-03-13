using Anchor.Application.Abstractions;
using Anchor.Domain;
using Microsoft.Extensions.Logging;

namespace Anchor.Recovery;

public sealed class SnapshotService : ISnapshotService
{
    private readonly IGitStatusReader _statusReader;
    private readonly IGitCommandExecutor _gitCommandExecutor;
    private readonly SnapshotStore _snapshotStore;
    private readonly ILogger<SnapshotService> _logger;

    public SnapshotService(
        IGitStatusReader statusReader,
        IGitCommandExecutor gitCommandExecutor,
        SnapshotStore snapshotStore,
        ILogger<SnapshotService> logger)
    {
        _statusReader = statusReader;
        _gitCommandExecutor = gitCommandExecutor;
        _snapshotStore = snapshotStore;
        _logger = logger;
    }

    public async Task<Snapshot> CreateAsync(string repositoryRoot, string description, CancellationToken cancellationToken)
    {
        var repoState = await _statusReader.ReadAsync(repositoryRoot, cancellationToken);
        var snapshotId = $"anchor-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..25];
        var refName = $"refs/anchor/snapshots/{snapshotId}";
        string? stashCommit = null;
        var includesWorkingTree = repoState.HasStagedChanges || repoState.HasUnstagedChanges || repoState.HasUntrackedFiles;

        var updateRefResult = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, ["update-ref", refName, "HEAD"], cancellationToken);
        if (!updateRefResult.IsSuccess)
        {
            throw new InvalidOperationException(updateRefResult.StandardError);
        }

        if (includesWorkingTree)
        {
            var stashMessage = $"anchor snapshot {snapshotId}: {description}";
            var stashResult = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, ["stash", "push", "--include-untracked", "-m", stashMessage], cancellationToken);
            if (stashResult.IsSuccess && !stashResult.StandardOutput.Contains("No local changes to save", StringComparison.OrdinalIgnoreCase))
            {
                var stashShaResult = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, ["rev-parse", "refs/stash"], cancellationToken);
                if (stashShaResult.IsSuccess)
                {
                    stashCommit = stashShaResult.StandardOutput.Trim();
                }
            }
        }

        var metadata = new SnapshotMetadata
        {
            Id = snapshotId,
            RepositoryId = string.Empty,
            RepositoryRoot = repositoryRoot,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
            HeadSha = repoState.HeadSha,
            BranchName = repoState.BranchName,
            RefName = refName,
            StashCommit = stashCommit,
            IncludesWorkingTree = includesWorkingTree,
            Notes = "Anchor snapshot using temporary refs and optional stash."
        };

        await _snapshotStore.SaveAsync(metadata, cancellationToken);
        _logger.LogInformation("Created snapshot {SnapshotId} for {RepositoryRoot}", snapshotId, repositoryRoot);

        return metadata.ToSnapshot();
    }
}
