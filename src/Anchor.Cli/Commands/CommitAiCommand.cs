using Anchor.Application.Abstractions;
using Anchor.Application.UseCases;
using Anchor.Domain;
using Anchor.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class CommitAiCommand : AsyncCommand<CommitAiCommand.Settings>
{
    public sealed class Settings : AnchorAiCommandSettings
    {
        [CommandOption("--staged")]
        public bool Staged { get; init; }

        [CommandOption("--all")]
        public bool All { get; init; }

        [CommandOption("--no-body")]
        public bool NoBody { get; init; }

        [CommandOption("--copy")]
        public bool Copy { get; init; }

        [CommandOption("--commit")]
        public bool Commit { get; init; }
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly CommitAiUseCase _commitAiUseCase;
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IGitCommandExecutor _gitCommandExecutor;
    private readonly IClipboardService _clipboardService;
    private readonly BannerRenderer _bannerRenderer;
    private readonly CommitSuggestionRenderer _renderer;
    private readonly ILocalizer _localizer;
    private readonly IAnsiConsole _console;

    public CommitAiCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        CommitAiUseCase commitAiUseCase,
        IGitRepositoryLocator repositoryLocator,
        IGitCommandExecutor gitCommandExecutor,
        IClipboardService clipboardService,
        BannerRenderer bannerRenderer,
        CommitSuggestionRenderer renderer,
        ILocalizer localizer,
        IAnsiConsole console)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _commitAiUseCase = commitAiUseCase;
        _repositoryLocator = repositoryLocator;
        _gitCommandExecutor = gitCommandExecutor;
        _clipboardService = clipboardService;
        _bannerRenderer = bannerRenderer;
        _renderer = renderer;
        _localizer = localizer;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Staged && settings.All)
        {
            _console.MarkupLine("[red]Use either --staged or --all.[/]");
            return 1;
        }

        var cancellationToken = CancellationToken.None;
        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, cancellationToken);
        _bannerRenderer.Render();

        CommitGenerationResult currentResult;
        try
        {
            currentResult = await _console.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(
                    _localizer.Get("CommitAi.Title", language.LanguageTag),
                    _ => _commitAiUseCase.ExecuteAsync(
                        new CommitGenerationRequest
                        {
                            UseStagedChanges = !settings.All,
                            IncludeAllChanges = settings.All,
                            IncludeBody = !settings.NoBody,
                            ProviderOverride = settings.Provider,
                            ModelOverride = settings.Model,
                            ResponseLanguage = language.LanguageTag,
                            CommitAfterAccept = settings.Commit,
                            CopyToClipboard = settings.Copy
                        },
                        language,
                        cancellationToken));
        }
        catch (Exception exception)
        {
            _console.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
            return 1;
        }

        var currentSuggestion = currentResult.PrimarySuggestion;

        while (true)
        {
            _renderer.Render(currentResult with { PrimarySuggestion = currentSuggestion }, language.LanguageTag);
            var action = AskCommitAction(language.LanguageTag);

            switch (action)
            {
                case "accept":
                    if (settings.Copy)
                    {
                        await CopySuggestionAsync(currentSuggestion, language.LanguageTag, cancellationToken);
                    }

                    if (settings.Commit)
                    {
                        return await CommitAsync(settings.All, currentSuggestion, language.LanguageTag, cancellationToken);
                    }

                    return 0;

                case "edit-title":
                    currentSuggestion = currentSuggestion with
                    {
                        Title = _console.Prompt(
                            new TextPrompt<string>("Title")
                                .DefaultValue(currentSuggestion.Title)
                                .AllowEmpty())
                    };
                    break;

                case "edit-body":
                    currentSuggestion = currentSuggestion with
                    {
                        Body = _console.Prompt(
                            new TextPrompt<string>("Body (use \\n for new lines)")
                                .DefaultValue(currentSuggestion.Body ?? string.Empty)
                                .AllowEmpty())
                            .Replace("\\n", Environment.NewLine, StringComparison.Ordinal)
                    };
                    break;

                case "regenerate":
                    currentResult = await _commitAiUseCase.ExecuteAsync(
                        new CommitGenerationRequest
                        {
                            UseStagedChanges = !settings.All,
                            IncludeAllChanges = settings.All,
                            IncludeBody = !settings.NoBody,
                            ProviderOverride = settings.Provider,
                            ModelOverride = settings.Model,
                            ResponseLanguage = language.LanguageTag
                        },
                        language,
                        cancellationToken);
                    currentSuggestion = currentResult.PrimarySuggestion;
                    break;

                case "copy":
                    await CopySuggestionAsync(currentSuggestion, language.LanguageTag, cancellationToken);
                    break;

                case "commit":
                    return await CommitAsync(settings.All, currentSuggestion, language.LanguageTag, cancellationToken);

                default:
                    return 130;
            }
        }
    }

    private string AskCommitAction(string language)
    {
        var actions = new Dictionary<string, string>
        {
            ["accept"] = _localizer.Get("Action.Accept", language),
            ["edit-title"] = _localizer.Get("Action.EditTitle", language),
            ["edit-body"] = _localizer.Get("Action.EditBody", language),
            ["regenerate"] = _localizer.Get("Action.Regenerate", language),
            ["copy"] = _localizer.Get("Action.Copy", language),
            ["commit"] = _localizer.Get("Action.Commit", language),
            ["cancel"] = _localizer.Get("Action.Cancel", language)
        };

        var choice = _console.Prompt(
            new SelectionPrompt<string>()
                .Title("[grey]Choose the next action[/]")
                .PageSize(10)
                .AddChoices(actions.Values));

        return actions.First(pair => pair.Value == choice).Key;
    }

    private async Task<int> CommitAsync(bool stageAllChanges, CommitSuggestion suggestion, string language, CancellationToken cancellationToken)
    {
        if (!_console.Prompt(new ConfirmationPrompt(_localizer.Get("CommitAi.CommitPrompt", language))))
        {
            return 130;
        }

        var repositoryRoot = await _repositoryLocator.LocateAsync(null, cancellationToken);

        if (stageAllChanges)
        {
            var addResult = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, ["add", "-A"], cancellationToken);
            if (!addResult.IsSuccess)
            {
                _console.MarkupLine($"[red]{Markup.Escape(addResult.StandardError)}[/]");
                return addResult.ExitCode;
            }
        }

        var arguments = new List<string> { "commit", "-m", suggestion.Header };
        if (!string.IsNullOrWhiteSpace(suggestion.Body))
        {
            arguments.Add("-m");
            arguments.Add(suggestion.Body);
        }

        if (suggestion.IsBreakingChange && !string.IsNullOrWhiteSpace(suggestion.BreakingChangeDescription))
        {
            arguments.Add("-m");
            arguments.Add($"BREAKING CHANGE: {suggestion.BreakingChangeDescription}");
        }

        var result = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, arguments, cancellationToken);
        if (result.IsSuccess)
        {
            _console.MarkupLine($"[green]{Markup.Escape(_localizer.Get("CommitAi.Committed", language))}[/]");
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                Console.WriteLine(result.StandardOutput);
            }

            return 0;
        }

        _console.MarkupLine($"[red]{Markup.Escape(result.StandardError)}[/]");
        return result.ExitCode;
    }

    private async Task CopySuggestionAsync(CommitSuggestion suggestion, string language, CancellationToken cancellationToken)
    {
        var copied = await _clipboardService.CopyAsync(suggestion.ToCommitMessage(), cancellationToken);
        _console.MarkupLine(
            copied
                ? $"[green]{Markup.Escape(_localizer.Get("CommitAi.Copied", language))}[/]"
                : $"[yellow]{Markup.Escape(_localizer.Get("CommitAi.CopyFailed", language))}[/]");
    }
}
