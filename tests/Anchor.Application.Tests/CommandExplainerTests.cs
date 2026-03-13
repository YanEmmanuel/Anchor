using Anchor.Application.Abstractions;
using Anchor.Application.Services;
using Anchor.Domain;

namespace Anchor.Application.Tests;

public sealed class CommandExplainerTests
{
    [Fact]
    public void Explain_ReturnsLocalizedResetDetails_WhenLanguageIsPortuguese()
    {
        var explainer = new CommandExplainer(new StubLocalizer());

        var result = explainer.Explain("reset --hard HEAD~1", "pt-BR");

        Assert.Equal("pt:Explain.Reset.Summary.Hard", result.Summary);
        Assert.Equal("pt:Explain.Reset.Head", result.HeadImpact);
        Assert.Equal("pt:Explain.Reset.Index.Hard", result.IndexImpact);
        Assert.Equal("pt:Explain.Reset.WorkingTree.Hard", result.WorkingTreeImpact);
        Assert.Equal("pt:Explain.Reset.Branch", result.BranchImpact);
        Assert.Equal("pt:Explain.Reset.Undo", result.UndoGuidance);
        Assert.Equal(RiskLevel.Critical, result.RiskLevel);
    }

    [Fact]
    public void Explain_FormatsGenericSummary_WithVerb()
    {
        var explainer = new CommandExplainer(new StubLocalizer());

        var result = explainer.Explain("bisect start", "en");

        Assert.Equal("en:Explain.Generic.Summary(bisect)", result.Summary);
        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
    }

    private sealed class StubLocalizer : ILocalizer
    {
        public string Get(string key, string? language = null, params object[] arguments)
        {
            var prefix = language?.StartsWith("pt", StringComparison.OrdinalIgnoreCase) == true ? "pt" : "en";
            return arguments.Length == 0
                ? $"{prefix}:{key}"
                : $"{prefix}:{key}({string.Join(", ", arguments)})";
        }
    }
}
