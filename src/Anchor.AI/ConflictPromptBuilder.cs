using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.AI;

public sealed class ConflictPromptBuilder : IConflictPromptBuilder
{
    public AIRequestContext Build(string repositoryRoot, IReadOnlyList<string> conflictedFiles, string language, string? providerOverride, string? modelOverride) =>
        new()
        {
            TaskName = "conflicts",
            Language = language,
            ProviderOverride = providerOverride,
            ModelOverride = modelOverride,
            SystemPrompt = $"Reply only in {language}. Explain merge or rebase conflicts with practical steps.",
            UserPrompt =
                $"Repository: {repositoryRoot}\nConflicted files:\n- {string.Join("\n- ", conflictedFiles)}\n" +
                "Explain what probably happened and suggest safe next steps."
        };
}
