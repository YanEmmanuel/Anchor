using System.Text;
using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.AI;

public sealed class SummaryPromptBuilder : ISummaryPromptBuilder
{
    public AIRequestContext BuildPullRequestSummary(string language, string baseBranch, DiffContent diff, IReadOnlyList<GitCommitSummary> recentCommits, string? providerOverride, string? modelOverride)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Base branch: {baseBranch}");
        prompt.AppendLine("Recent commits:");
        foreach (var commit in recentCommits.Take(10))
        {
            prompt.AppendLine($"- {commit.Subject}");
        }

        prompt.AppendLine();
        prompt.AppendLine("Diff:");
        prompt.AppendLine(diff.PatchText);

        return new AIRequestContext
        {
            TaskName = "pr-summary",
            Language = language,
            ProviderOverride = providerOverride,
            ModelOverride = modelOverride,
            SystemPrompt =
                $"Reply only in {language}. Return strict JSON with this schema: " +
                "{\"summary\":\"string\",\"changes\":[\"string\"],\"motivation\":\"string\",\"impact\":\"string\",\"testing\":\"string or null\"}. " +
                "Write a professional pull request description.",
            UserPrompt = prompt.ToString()
        };
    }

    public AIRequestContext BuildWorkSummary(string language, GitRepoContext repository, DiffContent diff, string? providerOverride, string? modelOverride) =>
        new()
        {
            TaskName = "summarize",
            Language = language,
            ProviderOverride = providerOverride,
            ModelOverride = modelOverride,
            SystemPrompt =
                $"Reply only in {language}. Return strict JSON with this schema: " +
                "{\"summary\":\"string\",\"highlights\":[\"string\"],\"risks\":[\"string\"]}. " +
                "Summarize the recent work for a daily update.",
            UserPrompt =
                $"Branch: {repository.BranchName}\nRecent commits:\n- {string.Join("\n- ", repository.RecentCommits.Select(static commit => commit.Subject))}\n\nDiff:\n{diff.PatchText}"
        };

    public AIRequestContext BuildWhyFile(string language, string filePath, DiffContent diff, IReadOnlyList<GitCommitSummary> fileHistory, string? providerOverride, string? modelOverride) =>
        new()
        {
            TaskName = "why-file",
            Language = language,
            ProviderOverride = providerOverride,
            ModelOverride = modelOverride,
            SystemPrompt =
                $"Reply only in {language}. Return strict JSON with this schema: " +
                "{\"summary\":\"string\",\"supportingCommits\":[\"string\"]}. " +
                "Explain why a file changed recently using commit history and diff context.",
            UserPrompt =
                $"File: {filePath}\nRecent commits:\n- {string.Join("\n- ", fileHistory.Select(static commit => commit.Subject))}\n\nCurrent diff:\n{diff.PatchText}"
        };

    public AIRequestContext BuildBranchName(string language, string goal, DiffContent diff, CommitIntentAnalysis analysis, string? providerOverride, string? modelOverride) =>
        new()
        {
            TaskName = "branch-ai",
            Language = language,
            ProviderOverride = providerOverride,
            ModelOverride = modelOverride,
            SystemPrompt =
                $"Reply only in {language}. Return strict JSON with this schema: " +
                "{\"name\":\"string\",\"alternatives\":[\"string\"],\"confidence\":0-100}. " +
                "Suggest concise branch names using git-friendly kebab-case.",
            UserPrompt =
                $"Goal: {goal}\nInferred type: {analysis.InferredType}\nScope: {analysis.InferredScope}\nFiles:\n- {string.Join("\n- ", diff.Files.Take(20))}\n\nDiff:\n{diff.PatchText}"
        };
}
