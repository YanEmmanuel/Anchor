using Anchor.Domain;
using Spectre.Console;

namespace Anchor.Presentation;

public sealed class SnapshotRenderer
{
    private readonly IAnsiConsole _console;

    public SnapshotRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(IReadOnlyList<RecoveryPoint> recoveryPoints)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Id");
        table.AddColumn("Created");
        table.AddColumn("Branch");
        table.AddColumn("Description");
        table.AddColumn("Working tree");

        foreach (var point in recoveryPoints)
        {
            table.AddRow(
                point.Id,
                point.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                point.BranchName ?? "-",
                Markup.Escape(point.Description),
                point.HasWorkingTreeSnapshot ? "yes" : "no");
        }

        _console.Write(table);
    }
}
