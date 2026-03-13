using Anchor.Application.Abstractions;
using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class SummarizeCommand : AsyncCommand<SummarizeCommand.Settings>
{
    public sealed class Settings : AnchorAiCommandSettings
    {
        [CommandOption("--copy")]
        public bool Copy { get; init; }
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly SummarizeUseCase _useCase;
    private readonly BannerRenderer _bannerRenderer;
    private readonly SummaryRenderer _renderer;
    private readonly IClipboardService _clipboardService;

    public SummarizeCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        SummarizeUseCase useCase,
        BannerRenderer bannerRenderer,
        SummaryRenderer renderer,
        IClipboardService clipboardService)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _useCase = useCase;
        _bannerRenderer = bannerRenderer;
        _renderer = renderer;
        _clipboardService = clipboardService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, CancellationToken.None);
        _bannerRenderer.Render();
        var result = await _useCase.ExecuteAsync(null, language.LanguageTag, settings.Provider, settings.Model, CancellationToken.None);
        _renderer.RenderWorkSummary(result);

        if (settings.Copy)
        {
            await _clipboardService.CopyAsync(result.Summary, CancellationToken.None);
        }

        return 0;
    }
}
