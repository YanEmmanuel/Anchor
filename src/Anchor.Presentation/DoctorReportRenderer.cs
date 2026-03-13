using Anchor.Application.Abstractions;
using Anchor.Domain;
using Spectre.Console;

namespace Anchor.Presentation;

public sealed class DoctorReportRenderer
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizer _localizer;

    public DoctorReportRenderer(IAnsiConsole console, ILocalizer localizer)
    {
        _console = console;
        _localizer = localizer;
    }

    public void Render(DoctorReport report, string language)
    {
        if (report.IsHealthy)
        {
            _console.Write(new Panel($"[green]{Markup.Escape(_localizer.Get("Doctor.Healthy", language))}[/]") { Border = BoxBorder.Rounded });
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Severity");
        table.AddColumn("Issue");
        table.AddColumn("Recommendation");

        foreach (var issue in report.Issues)
        {
            table.AddRow(
                GetSeverityMarkup(issue.Severity),
                $"[bold]{Markup.Escape(issue.Title)}[/]\n{Markup.Escape(issue.Details)}",
                Markup.Escape(issue.Recommendation));
        }

        _console.Write(table);
    }

    private static string GetSeverityMarkup(ProblemSeverity severity) =>
        severity switch
        {
            ProblemSeverity.Error => "[red]error[/]",
            ProblemSeverity.Warning => "[yellow]warning[/]",
            _ => "[blue]info[/]"
        };
}
