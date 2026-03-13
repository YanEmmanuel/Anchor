using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class UntrackedArtifactsDetector
{
    private static readonly string[] SuspiciousMarkers = ["bin/", "obj/", ".tmp", ".log", ".cache", "dist/", "coverage/"];

    public DoctorIssue? Detect(RepoState state)
    {
        var suspiciousFiles = state.UntrackedFiles
            .Where(static file => SuspiciousMarkers.Any(marker => file.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Take(5)
            .ToArray();

        if (suspiciousFiles.Length == 0)
        {
            return null;
        }

        return new DoctorIssue
        {
            Severity = ProblemSeverity.Warning,
            Code = "untracked-artifacts",
            Title = "Suspicious untracked artifacts",
            Details = $"Untracked build or temporary artifacts were detected: {string.Join(", ", suspiciousFiles)}",
            Recommendation = "Review whether these files should be ignored, cleaned, or committed intentionally."
        };
    }
}
