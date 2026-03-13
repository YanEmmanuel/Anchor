using System.Text;
using System.Text.RegularExpressions;
using Anchor.Domain;

namespace Anchor.Application.Services;

public sealed class CommitMessageFallbackGenerator
{
    private static readonly Regex AddedSymbolRegex = new(@"^\+\s*(public|internal|private|protected)?\s*(sealed|static|partial|async|\w+)*\s*(class|interface|record|enum|struct|Task<[^>]+>|Task|void)\s+(?<name>[A-Za-z0-9_]+)", RegexOptions.Compiled | RegexOptions.Multiline);

    public CommitSuggestion Generate(CommitIntentAnalysis analysis, DiffContent diff, string language)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var highlights = BuildHighlights(diff).ToArray();
        var title = BuildTitle(analysis, highlights, normalizedLanguage);
        var body = BuildBody(analysis, diff, highlights, normalizedLanguage);

        return new CommitSuggestion
        {
            Type = analysis.InferredType,
            Scope = analysis.InferredScope,
            Title = title,
            Body = body,
            Confidence = 55,
            Highlights = highlights,
            ConcernGroups = analysis.ConcernGroups
        };
    }

    public string ResolveCommitLanguage(UserLanguageContext userLanguage, Configuration.AnchorOptions options) =>
        options.CommitLanguageMode switch
        {
            CommitLanguageMode.English => "en",
            CommitLanguageMode.Custom when !string.IsNullOrWhiteSpace(options.CustomCommitLanguage) => NormalizeLanguage(options.CustomCommitLanguage!),
            _ => NormalizeLanguage(userLanguage.LanguageTag)
        };

    private static string BuildTitle(CommitIntentAnalysis analysis, IReadOnlyList<string> highlights, string language)
    {
        var topic = highlights.FirstOrDefault()
                    ?? analysis.DetectedModules.FirstOrDefault()
                    ?? analysis.InferredScope
                    ?? "repository";

        topic = topic.Replace('_', ' ').Replace('-', ' ').Trim();
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = "repository";
        }

        return language switch
        {
            "pt" => analysis.InferredType switch
            {
                "feat" => $"adiciona suporte a {topic}",
                "fix" => $"corrige {topic}",
                "docs" => $"atualiza a documentacao de {topic}",
                "test" => $"amplia a cobertura de {topic}",
                "ci" => $"ajusta pipeline para {topic}",
                "build" => $"atualiza configuracao de build para {topic}",
                "perf" => $"melhora a performance de {topic}",
                _ => $"refatora {topic}"
            },
            "es" => analysis.InferredType switch
            {
                "feat" => $"agrega soporte para {topic}",
                "fix" => $"corrige {topic}",
                "docs" => $"actualiza la documentacion de {topic}",
                "test" => $"amplia las pruebas de {topic}",
                "ci" => $"ajusta el pipeline para {topic}",
                "build" => $"actualiza la configuracion de build para {topic}",
                "perf" => $"mejora el rendimiento de {topic}",
                _ => $"refactoriza {topic}"
            },
            _ => analysis.InferredType switch
            {
                "feat" => $"add {topic} support",
                "fix" => $"fix {topic}",
                "docs" => $"update {topic} documentation",
                "test" => $"expand {topic} coverage",
                "ci" => $"adjust pipeline for {topic}",
                "build" => $"update build configuration for {topic}",
                "perf" => $"improve {topic} performance",
                _ => $"refactor {topic}"
            }
        };
    }

    private static string? BuildBody(CommitIntentAnalysis analysis, DiffContent diff, IReadOnlyList<string> highlights, string language)
    {
        var summary = language switch
        {
            "pt" => analysis.InferredType switch
            {
                "feat" => "Entrega uma nova capacidade alinhada ao contexto detectado no diff.",
                "fix" => "Resolve problemas observados no fluxo afetado e fortalece o comportamento atual.",
                "docs" => "Atualiza a documentacao para refletir o comportamento implementado.",
                "test" => "Amplia a cobertura automatizada para proteger o comportamento alterado.",
                "ci" => "Ajusta a automacao do repositorio para suportar o fluxo atual.",
                "build" => "Atualiza a configuracao de build e dependencias relacionadas.",
                "perf" => "Refina pontos sensiveis para reduzir custo e melhorar resposta.",
                _ => "Organiza a implementacao para deixar a base mais consistente."
            },
            "es" => analysis.InferredType switch
            {
                "feat" => "Entrega una nueva capacidad alineada con el contexto detectado en el diff.",
                "fix" => "Corrige problemas del flujo afectado y refuerza el comportamiento actual.",
                "docs" => "Actualiza la documentacion para reflejar el comportamiento implementado.",
                "test" => "Amplia la cobertura automatizada para proteger el comportamiento cambiado.",
                "ci" => "Ajusta la automatizacion del repositorio para soportar el flujo actual.",
                "build" => "Actualiza la configuracion de build y las dependencias relacionadas.",
                "perf" => "Refina puntos sensibles para reducir costo y mejorar la respuesta.",
                _ => "Reorganiza la implementacion para dejar la base mas consistente."
            },
            _ => analysis.InferredType switch
            {
                "feat" => "Introduce a new capability based on the intent inferred from the current change set.",
                "fix" => "Addresses the affected workflow and tightens the current behavior.",
                "docs" => "Refreshes the documentation so it matches the implemented behavior.",
                "test" => "Extends automated coverage to protect the updated behavior.",
                "ci" => "Adjusts repository automation to support the current workflow.",
                "build" => "Updates build configuration and dependency wiring around this change.",
                "perf" => "Refines hot paths to reduce cost and improve responsiveness.",
                _ => "Reshapes the implementation so the affected area stays easier to maintain."
            }
        };

        var bullets = highlights
            .Take(4)
            .Select(highlight => language switch
            {
                "pt" => $"- atualiza {highlight}",
                "es" => $"- actualiza {highlight}",
                _ => $"- update {highlight}"
            })
            .ToArray();

        if (diff.Files.Count > 0 && bullets.Length == 0)
        {
            bullets = diff.Files
                .Take(4)
                .Select(file => language switch
                {
                    "pt" => $"- ajusta {file}",
                    "es" => $"- ajusta {file}",
                    _ => $"- touch {file}"
                })
                .ToArray();
        }

        var builder = new StringBuilder(summary);

        if (bullets.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append(string.Join(Environment.NewLine, bullets));
        }

        return builder.ToString().Trim();
    }

    private static IReadOnlyList<string> BuildHighlights(DiffContent diff)
    {
        var symbolNames = AddedSymbolRegex.Matches(diff.PatchText)
            .Select(static match => match.Groups["name"].Value)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .ToList();

        if (symbolNames.Count > 0)
        {
            return symbolNames;
        }

        return diff.Files
            .Select(static file => Path.GetFileNameWithoutExtension(file))
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToArray();
    }

    private static string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        var normalized = language.Trim().ToLowerInvariant();
        if (normalized.StartsWith("pt", StringComparison.Ordinal))
        {
            return "pt";
        }

        if (normalized.StartsWith("es", StringComparison.Ordinal))
        {
            return "es";
        }

        return "en";
    }
}
