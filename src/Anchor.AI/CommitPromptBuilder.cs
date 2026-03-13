using System.Text;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.AI;

public sealed class CommitPromptBuilder : ICommitPromptBuilder
{
    private readonly IOptions<AnchorOptions> _options;

    public CommitPromptBuilder(IOptions<AnchorOptions> options)
    {
        _options = options;
    }

    public AIRequestContext Build(
        CommitGenerationRequest request,
        CommitIntentAnalysis intentAnalysis,
        DiffContent diff,
        IReadOnlyList<GitCommitSummary> recentCommits,
        string commitLanguage)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Repository context:");
        builder.AppendLine($"- inferredType: {intentAnalysis.InferredType}");
        builder.AppendLine($"- inferredScope: {intentAnalysis.InferredScope ?? "none"}");
        builder.AppendLine($"- mixedConcerns: {intentAnalysis.MixedConcerns}");
        builder.AppendLine($"- detectedModules: {string.Join(", ", intentAnalysis.DetectedModules)}");
        builder.AppendLine($"- filesChanged: {diff.Files.Count}");
        builder.AppendLine($"- addedLines: {diff.AddedLines}");
        builder.AppendLine($"- removedLines: {diff.RemovedLines}");
        builder.AppendLine();
        builder.AppendLine("Changed files:");
        foreach (var file in diff.Files.Take(30))
        {
            builder.AppendLine($"- {file}");
        }

        builder.AppendLine();
        builder.AppendLine("Recent commits:");
        foreach (var commit in recentCommits.Take(8))
        {
            builder.AppendLine($"- {commit.Subject}");
        }

        builder.AppendLine();
        builder.AppendLine("Diff:");
        builder.AppendLine(diff.PatchText);

        return new AIRequestContext
        {
            TaskName = "commit-ai",
            Language = commitLanguage,
            ProviderOverride = request.ProviderOverride,
            ModelOverride = request.ModelOverride,
            Temperature = 0.25d,
            MaxTokens = 1200,
            Timeout = TimeSpan.FromSeconds(_options.Value.AI.TimeoutSeconds),
            SystemPrompt =
                $"You are Anchor, a serious Git assistant. Reply only in {commitLanguage}. " +
                "Generate a professional Conventional Commit suggestion. " +
                "Return strict JSON with this schema: " +
                "{\"type\":\"feat|fix|refactor|perf|docs|test|chore|build|ci\",\"scope\":\"string or null\",\"title\":\"short lowercase title without trailing period\",\"body\":\"helpful body with optional bullets\",\"breakingChange\":true|false,\"breakingChangeDescription\":\"string or null\",\"highlights\":[\"string\"],\"confidence\":0-100}. " +
                "Be specific, avoid vague titles, and explain what changed and why when possible.",
            UserPrompt = builder.ToString()
        };
    }
}
