using Anchor.Application.Abstractions;
using Spectre.Console;

namespace Anchor.Presentation;

public sealed class BannerRenderer
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizer _localizer;

    public BannerRenderer(IAnsiConsole console, ILocalizer localizer)
    {
        _console = console;
        _localizer = localizer;
    }

    public void Render(string? language = null)
    {
        var rule = new Rule($"[bold steelblue1]Anchor[/] [grey]{Markup.Escape(_localizer.Get("Banner.Tagline", language))}[/]")
        {
            Justification = Justify.Left
        };

        _console.Write(rule);
    }
}
