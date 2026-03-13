using Anchor.Application.Abstractions;
using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class RecoverCommand : AsyncCommand<RecoverCommand.Settings>
{
    public sealed class Settings : AnchorCommandSettings
    {
        [CommandArgument(0, "[SNAPSHOT_ID]")]
        public string? SnapshotId { get; init; }
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly RecoverUseCase _recoverUseCase;
    private readonly BannerRenderer _bannerRenderer;
    private readonly SnapshotRenderer _renderer;
    private readonly ILocalizer _localizer;
    private readonly IAnsiConsole _console;

    public RecoverCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        RecoverUseCase recoverUseCase,
        BannerRenderer bannerRenderer,
        SnapshotRenderer renderer,
        ILocalizer localizer,
        IAnsiConsole console)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _recoverUseCase = recoverUseCase;
        _bannerRenderer = bannerRenderer;
        _renderer = renderer;
        _localizer = localizer;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, CancellationToken.None);
        _bannerRenderer.Render();

        if (string.IsNullOrWhiteSpace(settings.SnapshotId))
        {
            var recoveryPoints = await _recoverUseCase.ListAsync(null, CancellationToken.None);
            if (recoveryPoints.Count == 0)
            {
                _console.MarkupLine($"[yellow]{Markup.Escape(_localizer.Get("Recover.NoPoints", language.LanguageTag))}[/]");
                return 0;
            }

            _renderer.Render(recoveryPoints);
            return 0;
        }

        if (!_console.Prompt(new ConfirmationPrompt(_localizer.Get("Recover.RestorePrompt", language.LanguageTag))))
        {
            return 130;
        }

        try
        {
            var result = await _recoverUseCase.RestoreAsync(null, settings.SnapshotId, CancellationToken.None);
            if (result.IsSuccess)
            {
                _console.MarkupLine($"[green]{Markup.Escape(_localizer.Get("Recover.Restored", language.LanguageTag))}[/]");
                return 0;
            }

            _console.MarkupLine($"[red]{Markup.Escape(result.StandardError)}[/]");
            return result.ExitCode;
        }
        catch (Exception exception)
        {
            _console.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
            return 1;
        }
    }
}
