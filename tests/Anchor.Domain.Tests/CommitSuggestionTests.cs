using Anchor.Domain;

namespace Anchor.Domain.Tests;

public sealed class CommitSuggestionTests
{
    [Fact]
    public void ToCommitMessage_IncludesHeaderBodyAndBreakingChange()
    {
        var suggestion = new CommitSuggestion
        {
            Type = "feat",
            Scope = "auth",
            Title = "add refresh token support",
            Body = "Implement refresh token handling.",
            IsBreakingChange = true,
            BreakingChangeDescription = "Clients must send the new refresh token header."
        };

        var message = suggestion.ToCommitMessage();

        Assert.Contains("feat(auth): add refresh token support", message);
        Assert.Contains("Implement refresh token handling.", message);
        Assert.Contains("BREAKING CHANGE: Clients must send the new refresh token header.", message);
    }
}
