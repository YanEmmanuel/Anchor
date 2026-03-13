using Anchor.Application.Abstractions;
using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class GitPassthroughCommand : AsyncCommand<GitPassthroughCommand.Settings>
{
    public sealed class Settings : AnchorCommandSettings
    {
        [CommandOption("--yes")]
        public bool Yes { get; init; }

        [CommandArgument(0, "[ARGS]")]
        public string[] Arguments { get; init; } = [];
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly PreviewRiskyCommandUseCase _previewUseCase;
    private readonly ExecuteGitCommandUseCase _executeUseCase;
    private readonly IGitCommandExecutor _gitCommandExecutor;
    private readonly BannerRenderer _bannerRenderer;
    private readonly DangerousCommandRenderer _dangerousCommandRenderer;
    private readonly ILocalizer _localizer;
    private readonly IAnsiConsole _console;

    public GitPassthroughCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        PreviewRiskyCommandUseCase previewUseCase,
        ExecuteGitCommandUseCase executeUseCase,
        IGitCommandExecutor gitCommandExecutor,
        BannerRenderer bannerRenderer,
        DangerousCommandRenderer dangerousCommandRenderer,
        ILocalizer localizer,
        IAnsiConsole console)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _previewUseCase = previewUseCase;
        _executeUseCase = executeUseCase;
        _gitCommandExecutor = gitCommandExecutor;
        _bannerRenderer = bannerRenderer;
        _dangerousCommandRenderer = dangerousCommandRenderer;
        _localizer = localizer;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, CancellationToken.None);
        _bannerRenderer.Render();

        var arguments = settings.Arguments.Length == 0 ? new[] { "status" } : settings.Arguments;

        try
        {
            var analysis = await _previewUseCase.ExecuteAsync(null, arguments, CancellationToken.None);
            if (analysis.RequiresConfirmation)
            {
                _dangerousCommandRenderer.Render(analysis, $"git {string.Join(' ', arguments)}", null);
                if (!settings.Yes && !_console.Prompt(new ConfirmationPrompt(_localizer.Get("DangerousCommand.Confirm", language.LanguageTag))))
                {
                    return 130;
                }
            }

            var result = await _executeUseCase.ExecuteAsync(null, arguments, true, CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(result.SnapshotId))
            {
                _console.MarkupLine($"[green]{Markup.Escape(_localizer.Get("Snapshot.Created", language.LanguageTag, result.SnapshotId))}[/]");
            }

            WriteGitOutput(result.CommandResult);
            return result.CommandResult.ExitCode;
        }
        catch
        {
            var directResult = await _gitCommandExecutor.ExecuteAsync(Environment.CurrentDirectory, arguments, CancellationToken.None);
            WriteGitOutput(directResult);
            return directResult.ExitCode;
        }
    }

    private static void WriteGitOutput(Anchor.Domain.GitCommandResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            Console.WriteLine(result.StandardOutput);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            Console.Error.WriteLine(result.StandardError);
        }
    }
}
