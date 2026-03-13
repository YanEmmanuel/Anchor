using System.Globalization;
using System.Resources;
using Anchor.Application.Abstractions;

namespace Anchor.Localization;

public sealed class ResourceLocalizer : ILocalizer
{
    private readonly ResourceManager _resourceManager = new("Anchor.Localization.Resources.Messages", typeof(ResourceLocalizer).Assembly);

    public string Get(string key, string? language = null, params object[] arguments)
    {
        var culture = ResolveCulture(language);
        var template = _resourceManager.GetString(key, culture)
            ?? _resourceManager.GetString(key, CultureInfo.InvariantCulture)
            ?? key;

        return arguments.Length == 0
            ? template
            : string.Format(culture, template, arguments);
    }

    private static CultureInfo ResolveCulture(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return CultureInfo.InvariantCulture;
        }

        try
        {
            return CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            var normalized = language.Split('-', '_')[0];
            return CultureInfo.GetCultureInfo(normalized);
        }
    }
}
