using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class ConflictDetector
{
    public DoctorIssue? Detect(RepoState state) =>
        !state.HasConflicts
            ? null
            : new DoctorIssue
            {
                Severity = ProblemSeverity.Error,
                Code = "conflicts",
                Title = "Conflicts detected",
                Details = $"Git still has unresolved conflicts in: {string.Join(", ", state.ConflictFiles.Take(5))}",
                Recommendation = "Resolve the conflicted files and continue the interrupted operation before moving on."
            };
}
