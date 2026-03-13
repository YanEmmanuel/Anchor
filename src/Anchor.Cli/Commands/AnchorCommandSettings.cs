using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public abstract class AnchorCommandSettings : CommandSettings
{
    [CommandOption("--lang <LANG>")]
    public string? Language { get; init; }
}

public abstract class AnchorAiCommandSettings : AnchorCommandSettings
{
    [CommandOption("--provider <PROVIDER>")]
    public string? Provider { get; init; }

    [CommandOption("--model <MODEL>")]
    public string? Model { get; init; }
}
