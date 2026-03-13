using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class DoctorCommand : AsyncCommand<DoctorCommand.Settings>
{
    public sealed class Settings : AnchorCommandSettings
    {
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly DoctorUseCase _doctorUseCase;
    private readonly BannerRenderer _bannerRenderer;
    private readonly DoctorReportRenderer _renderer;
    private readonly IAnsiConsole _console;

    public DoctorCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        DoctorUseCase doctorUseCase,
        BannerRenderer bannerRenderer,
        DoctorReportRenderer renderer,
        IAnsiConsole console)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _doctorUseCase = doctorUseCase;
        _bannerRenderer = bannerRenderer;
        _renderer = renderer;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, CancellationToken.None);
        _bannerRenderer.Render();

        try
        {
            var report = await _console.Status()
                .StartAsync("Analyzing repository", _ => _doctorUseCase.ExecuteAsync(null, CancellationToken.None));
            _renderer.Render(report, language.LanguageTag);
            return report.IsHealthy ? 0 : 1;
        }
        catch (Exception exception)
        {
            _console.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
            return 1;
        }
    }
}
