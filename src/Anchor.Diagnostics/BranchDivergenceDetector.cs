using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class BranchDivergenceDetector
{
    public DoctorIssue? Detect(RepoState state)
    {
        if (state.AheadBy == 0 && state.BehindBy == 0)
        {
            return null;
        }

        return new DoctorIssue
        {
            Severity = state.BehindBy > 0 ? ProblemSeverity.Warning : ProblemSeverity.Info,
            Code = "branch-divergence",
            Title = "Branch divergence",
            Details = $"Current branch is ahead by {state.AheadBy} commit(s) and behind by {state.BehindBy} commit(s).",
            Recommendation = state.BehindBy > 0
                ? "Fetch and integrate remote changes before pushing."
                : "Push local commits when you are ready."
        };
    }
}
