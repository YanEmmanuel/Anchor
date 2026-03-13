using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class BranchAiCommand : AsyncCommand<BranchAiCommand.Settings>
{
    public sealed class Settings : AnchorAiCommandSettings
    {
        [CommandArgument(0, "[GOAL]")]
        public string[] GoalTokens { get; init; } = [];
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly BranchAiUseCase _useCase;
    private readonly BannerRenderer _bannerRenderer;
    private readonly SummaryRenderer _renderer;

    public BranchAiCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        BranchAiUseCase useCase,
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
        var result = await _useCase.ExecuteAsync(null, string.Join(' ', settings.GoalTokens), language.LanguageTag, settings.Provider, settings.Model, CancellationToken.None);
        _renderer.RenderBranchSuggestion(result);
        return 0;
    }
}
