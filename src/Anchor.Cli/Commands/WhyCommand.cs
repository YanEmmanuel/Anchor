using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class WhyCommand : AsyncCommand<WhyCommand.Settings>
{
    public sealed class Settings : AnchorAiCommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        public string FilePath { get; init; } = string.Empty;
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly WhyFileUseCase _useCase;
    private readonly BannerRenderer _bannerRenderer;
    private readonly SummaryRenderer _renderer;

    public WhyCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        WhyFileUseCase useCase,
        BannerRenderer bannerRenderer,
        SummaryRenderer renderer)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _useCase = useCase;
        _bannerRenderer = bannerRenderer;
        _renderer = renderer;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, CancellationToken.None);
        _bannerRenderer.Render();
        var result = await _useCase.ExecuteAsync(null, settings.FilePath, language.LanguageTag, settings.Provider, settings.Model, CancellationToken.None);
        _renderer.RenderWhyFile(result);
        return 0;
    }
}
