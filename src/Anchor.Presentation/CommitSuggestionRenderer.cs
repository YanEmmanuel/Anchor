using Anchor.Application.Abstractions;
using Anchor.Domain;
using Spectre.Console;

namespace Anchor.Presentation;

public sealed class CommitSuggestionRenderer
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizer _localizer;

    public CommitSuggestionRenderer(IAnsiConsole console, ILocalizer localizer)
    {
        _console = console;
        _localizer = localizer;
    }

    public void Render(CommitGenerationResult result, string language)
    {
        var suggestion = result.PrimarySuggestion;
        var title = result.UsedAI
            ? _localizer.Get("CommitAi.GeneratedByAi", language)
            : _localizer.Get("CommitAi.GeneratedByFallback", language);

        var panel = new Panel(new Markup(Markup.Escape(suggestion.ToCommitMessage())))
        {
            Header = new PanelHeader($"[bold]{Markup.Escape(title)}[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1)
        };

        _console.Write(panel);

        var details = new Table().Border(TableBorder.Rounded).AddColumn("Field").AddColumn("Value");
        details.AddRow("Type", suggestion.Type);
        details.AddRow("Scope", suggestion.Scope ?? _localizer.Get("General.None", language));
        details.AddRow("Confidence", $"{suggestion.Confidence}%");
        details.AddRow("Provider", result.ProviderName);
        details.AddRow("Model", result.Model);
        _console.Write(details);

        if (result.IntentAnalysis.MixedConcerns)
        {
            _console.Write(new Panel($"[yellow]{Markup.Escape(_localizer.Get("CommitAi.MixedConcerns", language))}[/]") { Border = BoxBorder.Rounded });
        }

        if (suggestion.Highlights.Count > 0)
        {
            var tree = new Tree("Highlights");
            foreach (var highlight in suggestion.Highlights)
            {
                tree.AddNode(Markup.Escape(highlight));
            }

            _console.Write(tree);
        }

        if (result.Warnings.Count > 0)
        {
            var warningPanel = new Panel(string.Join(Environment.NewLine, result.Warnings.Select(Markup.Escape)))
            {
                Header = new PanelHeader($"[yellow]{Markup.Escape(_localizer.Get("General.Warning", language))}[/]"),
                Border = BoxBorder.Rounded
            };
            _console.Write(warningPanel);
        }
    }
}
