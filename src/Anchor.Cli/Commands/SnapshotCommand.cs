using Anchor.Application.Abstractions;
using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class SnapshotCommand : AsyncCommand<SnapshotCommand.Settings>
{
    public sealed class Settings : AnchorCommandSettings
    {
        [CommandArgument(0, "[DESCRIPTION]")]
        public string? Description { get; init; }
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly SnapshotUseCase _snapshotUseCase;
    private readonly BannerRenderer _bannerRenderer;
    private readonly ILocalizer _localizer;
    private readonly IAnsiConsole _console;

    public SnapshotCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        SnapshotUseCase snapshotUseCase,
        BannerRenderer bannerRenderer,
        ILocalizer localizer,
        IAnsiConsole console)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _snapshotUseCase = snapshotUseCase;
        _bannerRenderer = bannerRenderer;
        _localizer = localizer;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, CancellationToken.None);
        _bannerRenderer.Render();

        try
        {
            var snapshot = await _snapshotUseCase.ExecuteAsync(
                null,
                settings.Description ?? $"Manual snapshot at {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}",
                CancellationToken.None);

            _console.MarkupLine($"[green]{Markup.Escape(_localizer.Get("Snapshot.Created", language.LanguageTag, snapshot.Id))}[/]");
            return 0;
        }
        catch (Exception exception)
        {
            _console.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
            return 1;
        }
    }
}
