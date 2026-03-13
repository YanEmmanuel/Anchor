namespace Anchor.Cli;

public static class ArgumentRouter
{
    private static readonly HashSet<string> BuiltInCommands =
    [
        "commit-ai",
        "explain",
        "doctor",
        "snapshot",
        "recover",
        "pr-summary",
        "summarize",
        "why",
        "branch-ai",
        "git",
        "help",
        "--help",
        "-h",
        "--version",
        "-v"
    ];

    public static string[] NormalizeArguments(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return ["git"];
        }

        return BuiltInCommands.Contains(arguments[0])
            ? arguments
            : ["git", .. arguments];
    }
}
