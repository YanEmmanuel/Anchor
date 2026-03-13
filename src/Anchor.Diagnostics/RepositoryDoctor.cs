using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class RepositoryDoctor : IRepositoryDoctor
{
    private readonly IGitStatusReader _statusReader;
    private readonly DetachedHeadDetector _detachedHeadDetector;
    private readonly InterruptedRebaseDetector _interruptedRebaseDetector;
    private readonly PendingMergeDetector _pendingMergeDetector;
    private readonly UntrackedArtifactsDetector _untrackedArtifactsDetector;
    private readonly BranchDivergenceDetector _branchDivergenceDetector;
    private readonly DirtyWorkingTreeDetector _dirtyWorkingTreeDetector;
    private readonly ConflictDetector _conflictDetector;

    public RepositoryDoctor(
        IGitStatusReader statusReader,
        DetachedHeadDetector detachedHeadDetector,
        InterruptedRebaseDetector interruptedRebaseDetector,
        PendingMergeDetector pendingMergeDetector,
        UntrackedArtifactsDetector untrackedArtifactsDetector,
        BranchDivergenceDetector branchDivergenceDetector,
        DirtyWorkingTreeDetector dirtyWorkingTreeDetector,
        ConflictDetector conflictDetector)
    {
        _statusReader = statusReader;
        _detachedHeadDetector = detachedHeadDetector;
        _interruptedRebaseDetector = interruptedRebaseDetector;
        _pendingMergeDetector = pendingMergeDetector;
        _untrackedArtifactsDetector = untrackedArtifactsDetector;
        _branchDivergenceDetector = branchDivergenceDetector;
        _dirtyWorkingTreeDetector = dirtyWorkingTreeDetector;
        _conflictDetector = conflictDetector;
    }

    public async Task<DoctorReport> AnalyzeAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var state = await _statusReader.ReadAsync(repositoryRoot, cancellationToken);
        var issues = new List<DoctorIssue>();

        AddIfPresent(issues, _detachedHeadDetector.Detect(state));
        AddIfPresent(issues, await _interruptedRebaseDetector.DetectAsync(repositoryRoot, cancellationToken));
        AddIfPresent(issues, await _pendingMergeDetector.DetectAsync(repositoryRoot, cancellationToken));
        AddIfPresent(issues, _dirtyWorkingTreeDetector.Detect(state));
        AddIfPresent(issues, _branchDivergenceDetector.Detect(state));
        AddIfPresent(issues, _untrackedArtifactsDetector.Detect(state));
        AddIfPresent(issues, _conflictDetector.Detect(state));

        return new DoctorReport
        {
            RepositoryRoot = repositoryRoot,
            BranchName = state.BranchName,
            RepoState = state,
            Issues = issues
                .OrderByDescending(static issue => issue.Severity)
                .ThenBy(static issue => issue.Code, StringComparer.Ordinal)
                .ToArray(),
            IsHealthy = issues.Count == 0
        };
    }

    private static void AddIfPresent(ICollection<DoctorIssue> issues, DoctorIssue? issue)
    {
        if (issue is not null)
        {
            issues.Add(issue);
        }
    }
}
