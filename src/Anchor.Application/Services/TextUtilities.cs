using System.Text.Json;
using System.Text.RegularExpressions;

namespace Anchor.Application.Services;

public static class TextUtilities
{
    public static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        var normalized = language.Trim().ToLowerInvariant();
        if (normalized.StartsWith("pt", StringComparison.Ordinal))
        {
            return "pt-BR";
        }

        if (normalized.StartsWith("es", StringComparison.Ordinal))
        {
            return "es";
        }

        return normalized;
    }

    public static string Slugify(string value)
    {
        var normalized = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "work" : normalized;
    }

    public static string ExtractJsonObject(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return "{}";
        }

        var firstBrace = rawContent.IndexOf('{');
        var lastBrace = rawContent.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
        {
            return "{}";
        }

        var candidate = rawContent[firstBrace..(lastBrace + 1)];
        using var _ = JsonDocument.Parse(candidate);
        return candidate;
    }
}
