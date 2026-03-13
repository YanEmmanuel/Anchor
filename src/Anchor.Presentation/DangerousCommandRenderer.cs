using Anchor.Domain;
using Spectre.Console;

namespace Anchor.Presentation;

public sealed class DangerousCommandRenderer
{
    private readonly IAnsiConsole _console;

    public DangerousCommandRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(CommandRiskAnalysis analysis, string commandText, string? snapshotId)
    {
        var table = new Table().Border(TableBorder.Rounded).AddColumn("Field").AddColumn("Value");
        table.AddRow("Command", Markup.Escape(commandText));
        table.AddRow("Risk", analysis.RiskLevel.ToString());
        table.AddRow("Snapshot", snapshotId ?? "-");
        table.AddRow("Summary", Markup.Escape(analysis.Summary));
        table.AddRow("Affected files", analysis.PotentiallyAffectedFiles.Count == 0 ? "-" : Markup.Escape(string.Join(", ", analysis.PotentiallyAffectedFiles)));
        _console.Write(table);

        if (analysis.Reasons.Count > 0)
        {
            var reasons = new Tree("Why Anchor flagged this");
            foreach (var reason in analysis.Reasons)
            {
                reasons.AddNode(Markup.Escape(reason));
            }

            _console.Write(reasons);
        }

        if (analysis.Alternatives.Count > 0)
        {
            var alternatives = new Tree("Safer alternatives");
            foreach (var alternative in analysis.Alternatives)
            {
                alternatives.AddNode(Markup.Escape(alternative));
            }

            _console.Write(alternatives);
        }
    }
}
