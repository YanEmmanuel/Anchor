using System.Text.RegularExpressions;
using Anchor.Domain;

namespace Anchor.Application.Services;

public sealed class CommitIntentAnalyzer
{
    private static readonly string[] DocumentationMarkers = ["docs/", "readme", ".md", "changelog"];
    private static readonly string[] TestMarkers = ["tests/", "test/", ".tests/", ".spec.", ".test."];
    private static readonly string[] CiMarkers = [".github/workflows", ".gitlab-ci", "azure-pipelines", "buildkite"];
    private static readonly string[] BuildMarkers = [".csproj", "directory.build", "dockerfile", "nuget.config", "package.json", "pnpm-lock", "yarn.lock"];
    private static readonly Regex SymbolRegex = new(@"^\+\s*(public|internal|private|protected)?\s*(sealed|static|partial|async|\w+)*\s*(class|interface|record|enum|struct)\s+(?<name>[A-Za-z0-9_]+)", RegexOptions.Compiled | RegexOptions.Multiline);

    public CommitIntentAnalysis Analyze(IReadOnlyList<string> files, string patchText)
    {
        var normalizedFiles = files
            .Where(static file => !string.IsNullOrWhiteSpace(file))
            .Select(NormalizePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var modules = normalizedFiles
            .Select(GuessModule)
            .Where(static module => !string.IsNullOrWhiteSpace(module))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToArray();

        var inferredScope = modules.Length == 1 ? modules[0] : modules.FirstOrDefault();
        var evidence = new List<string>();

        if (normalizedFiles.Length == 0)
        {
            return new CommitIntentAnalysis
            {
                InferredType = "chore",
                InferredScope = inferredScope,
                ChangedFileCount = 0
            };
        }

        var docsOnly = normalizedFiles.All(IsDocumentationFile);
        var testsOnly = normalizedFiles.All(IsTestFile);
        var ciOnly = normalizedFiles.All(IsCiFile);
        var buildOnly = normalizedFiles.All(IsBuildFile);
        var onlyRenames = patchText.Contains("rename from ", StringComparison.OrdinalIgnoreCase)
            && patchText.Contains("rename to ", StringComparison.OrdinalIgnoreCase)
            && !patchText.Contains(Environment.NewLine + "+", StringComparison.Ordinal);

        string inferredType;
        if (docsOnly)
        {
            inferredType = "docs";
            evidence.Add("all changes are documentation oriented");
        }
        else if (testsOnly)
        {
            inferredType = "test";
            evidence.Add("all changes target tests");
        }
        else if (ciOnly)
        {
            inferredType = "ci";
            evidence.Add("workflow or pipeline files were changed");
        }
        else if (buildOnly)
        {
            inferredType = "build";
            evidence.Add("build and dependency files dominate the change");
        }
        else if (onlyRenames)
        {
            inferredType = "refactor";
            evidence.Add("changes look like moves or renames");
        }
        else if (LooksLikeFix(normalizedFiles, patchText))
        {
            inferredType = "fix";
            evidence.Add("patch contains validation, null-safety or corrective patterns");
        }
        else if (LooksLikePerformanceWork(normalizedFiles, patchText))
        {
            inferredType = "perf";
            evidence.Add("patch references performance-sensitive code paths");
        }
        else if (LooksLikeFeatureWork(normalizedFiles, patchText))
        {
            inferredType = "feat";
            evidence.Add("patch adds behavior or new public surface");
        }
        else
        {
            inferredType = "refactor";
            evidence.Add("change reshapes implementation without a strong feature signal");
        }

        var concernGroups = normalizedFiles
            .Select(GuessConcernGroup)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(static group => !string.IsNullOrWhiteSpace(group))
            .ToArray();

        var symbolNames = SymbolRegex.Matches(patchText)
            .Select(static match => match.Groups["name"].Value)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToArray();

        evidence.AddRange(symbolNames.Select(static symbol => $"added or updated symbol `{symbol}`"));

        return new CommitIntentAnalysis
        {
            InferredType = inferredType,
            InferredScope = inferredScope,
            MixedConcerns = concernGroups.Length > 2 || modules.Length > 2,
            ChangedFileCount = normalizedFiles.Length,
            Evidence = evidence,
            ConcernGroups = concernGroups,
            DetectedModules = modules
        };
    }

    private static bool LooksLikeFix(IEnumerable<string> files, string patchText) =>
        files.Any(static file => file.Contains("bug", StringComparison.OrdinalIgnoreCase) || file.Contains("fix", StringComparison.OrdinalIgnoreCase))
        || patchText.Contains("throw ", StringComparison.OrdinalIgnoreCase)
        || patchText.Contains("ArgumentNullException", StringComparison.OrdinalIgnoreCase)
        || patchText.Contains("try", StringComparison.OrdinalIgnoreCase)
        || patchText.Contains("catch", StringComparison.OrdinalIgnoreCase)
        || patchText.Contains("validate", StringComparison.OrdinalIgnoreCase)
        || patchText.Contains("null", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeFeatureWork(IEnumerable<string> files, string patchText) =>
        files.Any(static file => file.Contains("controller", StringComparison.OrdinalIgnoreCase)
                                 || file.Contains("endpoint", StringComparison.OrdinalIgnoreCase)
                                 || file.Contains("service", StringComparison.OrdinalIgnoreCase))
        || patchText.Contains("new ", StringComparison.OrdinalIgnoreCase)
        || patchText.Contains("Add", StringComparison.Ordinal)
        || patchText.Contains("Create", StringComparison.Ordinal)
        || patchText.Contains("Enable", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikePerformanceWork(IEnumerable<string> files, string patchText) =>
        files.Any(static file => file.Contains("cache", StringComparison.OrdinalIgnoreCase))
        || patchText.Contains("Span<", StringComparison.Ordinal)
        || patchText.Contains("Memory<", StringComparison.Ordinal)
        || patchText.Contains("ArrayPool", StringComparison.Ordinal)
        || patchText.Contains("cache", StringComparison.OrdinalIgnoreCase)
        || patchText.Contains("performance", StringComparison.OrdinalIgnoreCase);

    private static bool IsDocumentationFile(string file) =>
        DocumentationMarkers.Any(marker => file.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static bool IsTestFile(string file) =>
        TestMarkers.Any(marker => file.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static bool IsCiFile(string file) =>
        CiMarkers.Any(marker => file.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static bool IsBuildFile(string file) =>
        BuildMarkers.Any(marker => file.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string GuessModule(string file)
    {
        var segments = NormalizePath(file).Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return "repo";
        }

        if (segments[0].Equals("src", StringComparison.OrdinalIgnoreCase) && segments.Length > 1)
        {
            return Slugify(segments[1].Replace("Anchor.", string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        if (segments[0].Equals("tests", StringComparison.OrdinalIgnoreCase) && segments.Length > 1)
        {
            return Slugify(segments[1].Replace(".Tests", string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        return Slugify(segments[0]);
    }

    private static string GuessConcernGroup(string file)
    {
        if (IsDocumentationFile(file))
        {
            return "docs";
        }

        if (IsTestFile(file))
        {
            return "tests";
        }

        if (IsCiFile(file))
        {
            return "ci";
        }

        if (IsBuildFile(file))
        {
            return "build";
        }

        return GuessModule(file);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').Trim();

    private static string Slugify(string value) =>
        Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
}
