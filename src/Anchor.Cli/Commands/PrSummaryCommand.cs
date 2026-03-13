using System.Text;
using Anchor.Application.Abstractions;
using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class PrSummaryCommand : AsyncCommand<PrSummaryCommand.Settings>
{
    public sealed class Settings : AnchorAiCommandSettings
    {
        [CommandOption("--copy")]
        public bool Copy { get; init; }
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly PrSummaryUseCase _useCase;
    private readonly BannerRenderer _bannerRenderer;
    private readonly SummaryRenderer _renderer;
    private readonly IClipboardService _clipboardService;
    private readonly IAnsiConsole _console;

    public PrSummaryCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        PrSummaryUseCase useCase,
        BannerRenderer bannerRenderer,
        SummaryRenderer renderer,
        IClipboardService clipboardService,
        IAnsiConsole console)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _useCase = useCase;
        _bannerRenderer = bannerRenderer;
        _renderer = renderer;
        _clipboardService = clipboardService;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, CancellationToken.None);
        _bannerRenderer.Render();
        var result = await _useCase.ExecuteAsync(null, language.LanguageTag, settings.Provider, settings.Model, CancellationToken.None);
        _renderer.RenderPullRequestSummary(result);

        if (settings.Copy)
        {
            await _clipboardService.CopyAsync(Format(result), CancellationToken.None);
        }

        return 0;
    }

    private static string Format(Anchor.Domain.PullRequestSummaryResult result)
    {
        var summary = result.Summary;
        var builder = new StringBuilder();
        builder.AppendLine("Summary");
        builder.AppendLine(summary.Summary);
        builder.AppendLine();
        builder.AppendLine("Changes");
        foreach (var change in summary.Changes)
        {
            builder.AppendLine($"- {change}");
        }

        builder.AppendLine();
        builder.AppendLine("Motivation");
        builder.AppendLine(summary.Motivation);
        builder.AppendLine();
        builder.AppendLine("Impact");
        builder.AppendLine(summary.Impact);
        builder.AppendLine();
        builder.AppendLine("Testing");
        builder.AppendLine(summary.Testing ?? "-");
        return builder.ToString().TrimEnd();
    }
}
