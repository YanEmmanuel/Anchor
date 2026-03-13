using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.AI;

public sealed class ExplainPromptBuilder : IExplainPromptBuilder
{
    public AIRequestContext Build(string commandText, CommandExplanation explanation, string language, string? providerOverride, string? modelOverride) =>
        new()
        {
            TaskName = "explain",
            Language = language,
            ProviderOverride = providerOverride,
            ModelOverride = modelOverride,
            SystemPrompt = $"Reply only in {language}. Improve the explanation for a Git command without inventing guarantees. Keep it precise.",
            UserPrompt =
                $"Command: {commandText}\n" +
                $"Summary: {explanation.Summary}\n" +
                $"HEAD: {explanation.HeadImpact}\n" +
                $"Index: {explanation.IndexImpact}\n" +
                $"Working tree: {explanation.WorkingTreeImpact}\n" +
                $"Branch: {explanation.BranchImpact}\n" +
                $"Risk: {explanation.RiskLevel}\n" +
                $"Undo: {explanation.UndoGuidance}\n" +
                "Return a tighter explanation in plain text."
        };
}
