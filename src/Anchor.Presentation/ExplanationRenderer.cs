using Anchor.Application.Abstractions;
using Anchor.Domain;
using Spectre.Console;

namespace Anchor.Presentation;

public sealed class ExplanationRenderer
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizer _localizer;

    public ExplanationRenderer(IAnsiConsole console, ILocalizer localizer)
    {
        _console = console;
        _localizer = localizer;
    }

    public void Render(CommandExplanation explanation, string language)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow($"[grey]{Markup.Escape(_localizer.Get("Explain.Label.Summary", language))}[/]", Markup.Escape(explanation.Summary));
        grid.AddRow($"[grey]{Markup.Escape(_localizer.Get("Explain.Label.Head", language))}[/]", Markup.Escape(explanation.HeadImpact));
        grid.AddRow($"[grey]{Markup.Escape(_localizer.Get("Explain.Label.Index", language))}[/]", Markup.Escape(explanation.IndexImpact));
        grid.AddRow($"[grey]{Markup.Escape(_localizer.Get("Explain.Label.WorkingTree", language))}[/]", Markup.Escape(explanation.WorkingTreeImpact));
        grid.AddRow($"[grey]{Markup.Escape(_localizer.Get("Explain.Label.Branch", language))}[/]", Markup.Escape(explanation.BranchImpact));
        grid.AddRow($"[grey]{Markup.Escape(_localizer.Get("Explain.Label.Risk", language))}[/]", Markup.Escape(_localizer.Get($"Risk.{explanation.RiskLevel}", language)));
        grid.AddRow($"[grey]{Markup.Escape(_localizer.Get("Explain.Label.Undo", language))}[/]", Markup.Escape(explanation.UndoGuidance));

        var panel = new Panel(grid)
        {
            Header = new PanelHeader($"[bold]{Markup.Escape(explanation.CommandText)}[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1)
        };

        _console.Write(panel);

        if (explanation.Notes.Count > 0)
        {
            var notes = new Tree(_localizer.Get("Explain.Label.Notes", language));
            foreach (var note in explanation.Notes)
            {
                notes.AddNode(Markup.Escape(note));
            }

            _console.Write(notes);
        }
    }
}
