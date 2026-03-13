using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class InterruptedRebaseDetector
{
    public Task<DoctorIssue?> DetectAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var gitDirectory = ResolveGitDirectory(repositoryRoot);
        var hasRebaseState = Directory.Exists(Path.Combine(gitDirectory, "rebase-merge"))
                             || Directory.Exists(Path.Combine(gitDirectory, "rebase-apply"));

        return Task.FromResult(
            hasRebaseState
                ? new DoctorIssue
                {
                    Severity = ProblemSeverity.Error,
                    Code = "rebase-interrupted",
                    Title = "Interrupted rebase",
                    Details = "A rebase is still in progress.",
                    Recommendation = "Continue with git rebase --continue, resolve conflicts, or abort with git rebase --abort."
                }
                : null);
    }

    private static string ResolveGitDirectory(string repositoryRoot)
    {
        var dotGit = Path.Combine(repositoryRoot, ".git");
        return Directory.Exists(dotGit) ? dotGit : repositoryRoot;
    }
}
