using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class DirtyWorkingTreeDetector
{
    public DoctorIssue? Detect(RepoState state)
    {
        if (!state.HasStagedChanges && !state.HasUnstagedChanges && !state.HasUntrackedFiles)
        {
            return null;
        }

        return new DoctorIssue
        {
            Severity = ProblemSeverity.Info,
            Code = "dirty-working-tree",
            Title = "Dirty working tree",
            Details = $"Detected {state.ChangedFiles.Count} changed file(s) and {state.UntrackedFiles.Count} untracked file(s).",
            Recommendation = "Review local changes before risky operations like rebase, reset or branch switches."
        };
    }
}
