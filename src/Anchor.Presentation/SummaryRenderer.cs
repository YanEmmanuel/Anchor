using Anchor.Domain;
using Spectre.Console;

namespace Anchor.Presentation;

public sealed class SummaryRenderer
{
    private readonly IAnsiConsole _console;

    public SummaryRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void RenderPullRequestSummary(PullRequestSummaryResult result)
    {
        var summary = result.Summary;
        var sections = new Rows(
            new Markup($"[bold]Summary[/]\n{Markup.Escape(summary.Summary)}"),
            new Markup($"[bold]Changes[/]\n{Markup.Escape(string.Join(Environment.NewLine, summary.Changes.Select(static change => $"- {change}")))}"),
            new Markup($"[bold]Motivation[/]\n{Markup.Escape(summary.Motivation)}"),
            new Markup($"[bold]Impact[/]\n{Markup.Escape(summary.Impact)}"),
            new Markup($"[bold]Testing[/]\n{Markup.Escape(summary.Testing ?? "-")}")
        );

        _console.Write(new Panel(sections) { Border = BoxBorder.Rounded });
    }

    public void RenderWorkSummary(WorkSummaryResult result)
    {
        var panel = new Panel(Markup.Escape(result.Summary))
        {
            Border = BoxBorder.Rounded
        };
        _console.Write(panel);

        if (result.Highlights.Count > 0)
        {
            var highlights = new Tree("Highlights");
            foreach (var highlight in result.Highlights)
            {
                highlights.AddNode(Markup.Escape(highlight));
            }

            _console.Write(highlights);
        }

        if (result.Risks.Count > 0)
        {
            var risks = new Tree("Risks");
            foreach (var risk in result.Risks)
            {
                risks.AddNode(Markup.Escape(risk));
            }

            _console.Write(risks);
        }
    }

    public void RenderWhyFile(WhyFileResult result)
    {
        _console.Write(new Panel(Markup.Escape(result.Summary)) { Border = BoxBorder.Rounded });

        if (result.SupportingCommits.Count > 0)
        {
            var table = new Table().Border(TableBorder.Rounded).AddColumn("Supporting commits");
            foreach (var commit in result.SupportingCommits)
            {
                table.AddRow(Markup.Escape(commit));
            }

            _console.Write(table);
        }
    }

    public void RenderBranchSuggestion(BranchNameSuggestion suggestion)
    {
        var table = new Table().Border(TableBorder.Rounded).AddColumn("Suggestion").AddColumn("Confidence");
        table.AddRow(Markup.Escape(suggestion.Name), $"{suggestion.Confidence}%");
        _console.Write(table);

        if (suggestion.Alternatives.Count > 0)
        {
            var tree = new Tree("Alternatives");
            foreach (var alternative in suggestion.Alternatives)
            {
                tree.AddNode(Markup.Escape(alternative));
            }

            _console.Write(tree);
        }
    }
}
