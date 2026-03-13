using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class DetachedHeadDetector
{
    public DoctorIssue? Detect(RepoState state) =>
        !state.IsDetachedHead
            ? null
            : new DoctorIssue
            {
                Severity = ProblemSeverity.Warning,
                Code = "detached-head",
                Title = "Detached HEAD",
                Details = "You are not currently on a named branch, so new commits may become harder to find later.",
                Recommendation = "Create or switch to a branch before making more commits if you want to keep this work."
            };
}
