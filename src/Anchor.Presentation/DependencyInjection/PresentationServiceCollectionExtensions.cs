using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Anchor.Presentation.DependencyInjection;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorPresentationServices(this IServiceCollection services)
    {
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
        services.AddSingleton<BannerRenderer>();
        services.AddSingleton<CommitSuggestionRenderer>();
        services.AddSingleton<DoctorReportRenderer>();
        services.AddSingleton<ExplanationRenderer>();
        services.AddSingleton<SnapshotRenderer>();
        services.AddSingleton<SummaryRenderer>();
        services.AddSingleton<DangerousCommandRenderer>();
        return services;
    }
}
