using Anchor.Cli;

namespace Anchor.Cli.Tests;

public sealed class ArgumentRouterTests
{
    [Fact]
    public void NormalizeArguments_RoutesUnknownCommandsThroughGit()
    {
        var normalized = ArgumentRouter.NormalizeArguments(["status"]);

        Assert.Equal(["git", "status"], normalized);
    }

    [Fact]
    public void NormalizeArguments_LeavesBuiltInCommandsUntouched()
    {
        var normalized = ArgumentRouter.NormalizeArguments(["doctor"]);

        Assert.Equal(["doctor"], normalized);
    }
}
