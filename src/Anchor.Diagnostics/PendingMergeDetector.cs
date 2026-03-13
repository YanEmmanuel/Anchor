using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class PendingMergeDetector
{
    public Task<DoctorIssue?> DetectAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var gitDirectory = ResolveGitDirectory(repositoryRoot);
        var mergeHeadPath = Path.Combine(gitDirectory, "MERGE_HEAD");

        return Task.FromResult(
            File.Exists(mergeHeadPath)
                ? new DoctorIssue
                {
                    Severity = ProblemSeverity.Error,
                    Code = "merge-pending",
                    Title = "Merge in progress",
                    Details = "Git found merge state markers in the repository.",
                    Recommendation = "Resolve the merge and commit it, or abort with git merge --abort."
                }
                : null);
    }

    private static string ResolveGitDirectory(string repositoryRoot)
    {
        var dotGit = Path.Combine(repositoryRoot, ".git");
        return Directory.Exists(dotGit) ? dotGit : repositoryRoot;
    }
}
