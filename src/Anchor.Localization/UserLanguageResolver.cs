using System.Globalization;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Application.Services;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.Localization;

public sealed class UserLanguageResolver : IUserLanguageResolver
{
    private readonly IOptions<AnchorOptions> _options;
    private readonly IEnvironmentReader _environmentReader;

    public UserLanguageResolver(IOptions<AnchorOptions> options, IEnvironmentReader environmentReader)
    {
        _options = options;
        _environmentReader = environmentReader;
    }

    public ValueTask<UserLanguageContext> ResolveAsync(string? commandLineOverride, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var configurationLanguage = _options.Value.Language;
        if (IsExplicit(configurationLanguage))
        {
            return ValueTask.FromResult(BuildContext(configurationLanguage!, LanguageDetectionSource.Configuration));
        }

        if (IsExplicit(commandLineOverride))
        {
            return ValueTask.FromResult(BuildContext(commandLineOverride!, LanguageDetectionSource.CommandLine));
        }

        var environmentLanguage = _environmentReader.GetEnvironmentVariable("ANCHOR_LANG");
        if (IsExplicit(environmentLanguage))
        {
            return ValueTask.FromResult(BuildContext(environmentLanguage!, LanguageDetectionSource.Environment));
        }

        if (!string.IsNullOrWhiteSpace(CultureInfo.CurrentUICulture.Name))
        {
            return ValueTask.FromResult(BuildContext(CultureInfo.CurrentUICulture.Name, LanguageDetectionSource.CurrentUiCulture));
        }

        if (!string.IsNullOrWhiteSpace(CultureInfo.CurrentCulture.Name))
        {
            return ValueTask.FromResult(BuildContext(CultureInfo.CurrentCulture.Name, LanguageDetectionSource.CurrentCulture));
        }

        var langEnvironment = _environmentReader.GetEnvironmentVariable("LANG");
        if (IsExplicit(langEnvironment))
        {
            return ValueTask.FromResult(BuildContext(langEnvironment!, LanguageDetectionSource.LangEnvironment));
        }

        return ValueTask.FromResult(BuildContext("en", LanguageDetectionSource.Fallback));
    }

    private static bool IsExplicit(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Equals("auto", StringComparison.OrdinalIgnoreCase);

    private static UserLanguageContext BuildContext(string rawLanguage, LanguageDetectionSource source)
    {
        var normalized = Normalize(rawLanguage);
        var culture = GetCulture(normalized);

        return new UserLanguageContext
        {
            LanguageTag = normalized,
            DisplayName = culture.NativeName,
            Source = source
        };
    }

    private static string Normalize(string rawLanguage)
    {
        var sanitized = rawLanguage.Trim();
        if (sanitized.Contains('.', StringComparison.Ordinal))
        {
            sanitized = sanitized[..sanitized.IndexOf('.', StringComparison.Ordinal)];
        }

        sanitized = sanitized.Replace('_', '-');
        sanitized = TextUtilities.NormalizeLanguage(sanitized);
        return sanitized;
    }

    private static CultureInfo GetCulture(string language)
    {
        try
        {
            return CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo(language.Split('-')[0]);
        }
    }
}
