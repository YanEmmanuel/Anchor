using Anchor.AI;
using Anchor.Application.Configuration;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.AI.Tests;

public sealed class CommitPromptBuilderTests
{
    [Fact]
    public void Build_IncludesJsonSchemaAndDiffContext()
    {
        var builder = new CommitPromptBuilder(Options.Create(new AnchorOptions()));

        var prompt = builder.Build(
            new CommitGenerationRequest
            {
                ProviderOverride = "ollama",
                ModelOverride = "qwen2.5:14b-instruct"
            },
            new CommitIntentAnalysis
            {
                InferredType = "feat",
                InferredScope = "auth",
                DetectedModules = ["auth"]
            },
            new DiffContent
            {
                Files = ["src/Anchor.Auth/LoginService.cs"],
                PatchText = "+ public class LoginService",
                AddedLines = 10,
                RemovedLines = 2
            },
            [new GitCommitSummary { Subject = "refactor auth flow" }],
            "pt-BR");

        Assert.Contains("Return strict JSON", prompt.SystemPrompt);
        Assert.Contains("src/Anchor.Auth/LoginService.cs", prompt.UserPrompt);
        Assert.Contains("refactor auth flow", prompt.UserPrompt);
        Assert.Equal("pt-BR", prompt.Language);
    }
}
