using Anchor.Domain;

namespace Anchor.Application.Services;

public sealed class SummaryFallbackComposer
{
    public PullRequestSummary BuildPullRequestSummary(string baseBranch, DiffContent diff, IReadOnlyList<GitCommitSummary> recentCommits, string language)
    {
        var normalizedLanguage = Normalize(language);
        var summary = normalizedLanguage switch
        {
            "pt" => $"Esta branch consolida {diff.Files.Count} arquivo(s) alterado(s) em relacao a {baseBranch}.",
            "es" => $"Esta rama consolida {diff.Files.Count} archivo(s) cambiado(s) respecto de {baseBranch}.",
            _ => $"This branch updates {diff.Files.Count} file(s) compared with {baseBranch}."
        };

        return new PullRequestSummary
        {
            Summary = summary,
            Changes = diff.Files.Take(6).Select(file => PhraseFile(language, file)).ToArray(),
            Motivation = recentCommits.Count > 0 ? recentCommits[0].Subject : FallbackPhrase(language, "Keeps the current line of work moving forward."),
            Impact = ImpactPhrase(language, diff),
            Testing = DetectTesting(diff.Files, language)
        };
    }

    public WorkSummaryResult BuildWorkSummary(GitRepoContext repository, DiffContent diff, string language) =>
        new()
        {
            Summary = Normalize(language) switch
            {
                "pt" => $"Trabalho recente na branch {repository.BranchName ?? "atual"} com {diff.Files.Count} arquivo(s) em movimento.",
                "es" => $"Trabajo reciente en la rama {repository.BranchName ?? "actual"} con {diff.Files.Count} archivo(s) en movimiento.",
                _ => $"Recent work on branch {repository.BranchName ?? "current"} touched {diff.Files.Count} file(s)."
            },
            Highlights = repository.RecentCommits.Take(5).Select(static commit => commit.Subject).ToArray(),
            Risks = repository.State.HasConflicts
                ? [FallbackPhrase(language, "Unresolved conflicts still need attention.")]
                : repository.State.HasUntrackedFiles
                    ? [FallbackPhrase(language, "There are untracked files worth reviewing before risky Git operations.")]
                    : Array.Empty<string>()
        };

    public WhyFileResult BuildWhyFile(string filePath, IReadOnlyList<GitCommitSummary> history, string language) =>
        new()
        {
            FilePath = filePath,
            Summary = history.Count == 0
                ? FallbackPhrase(language, "No recent history was found for this file.")
                : Normalize(language) switch
                {
                    "pt" => $"As ultimas mudancas em {filePath} giram em torno de: {history[0].Subject}",
                    "es" => $"Los ultimos cambios en {filePath} giran alrededor de: {history[0].Subject}",
                    _ => $"Recent changes in {filePath} are mainly about: {history[0].Subject}"
                },
            SupportingCommits = history.Take(5).Select(commit => $"{commit.Sha[..Math.Min(7, commit.Sha.Length)]} {commit.Subject}").ToArray()
        };

    public BranchNameSuggestion BuildBranchSuggestion(string goal, CommitIntentAnalysis analysis, string language)
    {
        var prefix = analysis.InferredType switch
        {
            "feat" => "feature",
            "fix" => "fix",
            "refactor" => "refactor",
            "docs" => "docs",
            "test" => "test",
            "build" => "build",
            "ci" => "ci",
            "perf" => "perf",
            _ => "chore"
        };

        var slugSource = string.IsNullOrWhiteSpace(goal)
            ? analysis.InferredScope ?? analysis.DetectedModules.FirstOrDefault() ?? "work"
            : goal;

        var slug = TextUtilities.Slugify(slugSource);
        var name = $"{prefix}/{slug}";

        return new BranchNameSuggestion
        {
            Prefix = prefix,
            Slug = slug,
            Name = name,
            Confidence = 65,
            Alternatives =
            [
                $"{prefix}/{slug}-cleanup",
                $"{prefix}/{slug}-update"
            ],
            UsedAI = false
        };
    }

    private static string DetectTesting(IReadOnlyList<string> files, string language)
    {
        var hasTests = files.Any(static file => file.Contains("test", StringComparison.OrdinalIgnoreCase));
        if (!hasTests)
        {
            return Normalize(language) switch
            {
                "pt" => "Nao foi possivel detectar testes automaticamente.",
                "es" => "No fue posible detectar pruebas automaticamente.",
                _ => "No obvious automated testing signal was detected."
            };
        }

        return Normalize(language) switch
        {
            "pt" => "Foram detectados arquivos de teste no diff.",
            "es" => "Se detectaron archivos de prueba en el diff.",
            _ => "Test files were detected in the diff."
        };
    }

    private static string PhraseFile(string language, string file) =>
        Normalize(language) switch
        {
            "pt" => $"Ajusta {file}",
            "es" => $"Ajusta {file}",
            _ => $"Updates {file}"
        };

    private static string ImpactPhrase(string language, DiffContent diff) =>
        Normalize(language) switch
        {
            "pt" => $"Impacta {diff.Files.Count} arquivo(s) com {diff.AddedLines} linha(s) adicionada(s) e {diff.RemovedLines} removida(s).",
            "es" => $"Impacta {diff.Files.Count} archivo(s) con {diff.AddedLines} linea(s) agregada(s) y {diff.RemovedLines} eliminada(s).",
            _ => $"Touches {diff.Files.Count} file(s) with {diff.AddedLines} added line(s) and {diff.RemovedLines} removed line(s)."
        };

    private static string FallbackPhrase(string language, string english) =>
        Normalize(language) switch
        {
            "pt" => english switch
            {
                "Keeps the current line of work moving forward." => "Mantem a linha atual de trabalho avancando.",
                "Unresolved conflicts still need attention." => "Ainda existem conflitos sem resolver que exigem atencao.",
                "There are untracked files worth reviewing before risky Git operations." => "Existem arquivos nao rastreados que valem uma revisao antes de operacoes Git arriscadas.",
                "No recent history was found for this file." => "Nenhum historico recente foi encontrado para este arquivo.",
                _ => english
            },
            "es" => english switch
            {
                "Keeps the current line of work moving forward." => "Mantiene la linea actual de trabajo avanzando.",
                "Unresolved conflicts still need attention." => "Todavia hay conflictos sin resolver que requieren atencion.",
                "There are untracked files worth reviewing before risky Git operations." => "Hay archivos no rastreados que conviene revisar antes de operaciones Git riesgosas.",
                "No recent history was found for this file." => "No se encontro historial reciente para este archivo.",
                _ => english
            },
            _ => english
        };

    private static string Normalize(string language)
    {
        if (language.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            return "pt";
        }

        return language.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? "es" : "en";
    }
}
