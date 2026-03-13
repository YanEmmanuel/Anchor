using Anchor.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Localization.DependencyInjection;

public static class LocalizationServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorLocalizationServices(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizer, ResourceLocalizer>();
        services.AddSingleton<IUserLanguageResolver, UserLanguageResolver>();
        return services;
    }
}
