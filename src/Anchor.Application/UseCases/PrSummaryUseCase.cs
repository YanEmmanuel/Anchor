using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Application.Services;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.Application.UseCases;

public sealed class PrSummaryUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IGitBranchReader _branchReader;
    private readonly IGitDiffReader _diffReader;
    private readonly IGitLogReader _logReader;
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly ISummaryPromptBuilder _summaryPromptBuilder;
    private readonly SummaryFallbackComposer _fallbackComposer;
    private readonly IOptions<AnchorOptions> _options;

    public PrSummaryUseCase(
        IGitRepositoryLocator repositoryLocator,
        IGitBranchReader branchReader,
        IGitDiffReader diffReader,
        IGitLogReader logReader,
        IAIProviderFactory aiProviderFactory,
        ISummaryPromptBuilder summaryPromptBuilder,
        SummaryFallbackComposer fallbackComposer,
        IOptions<AnchorOptions> options)
    {
        _repositoryLocator = repositoryLocator;
        _branchReader = branchReader;
        _diffReader = diffReader;
        _logReader = logReader;
        _aiProviderFactory = aiProviderFactory;
        _summaryPromptBuilder = summaryPromptBuilder;
        _fallbackComposer = fallbackComposer;
        _options = options;
    }

    public async Task<PullRequestSummaryResult> ExecuteAsync(string? startPath, string language, string? providerOverride, string? modelOverride, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        var baseBranch = await _branchReader.ResolveBaseBranchAsync(repositoryRoot, cancellationToken);
        var diff = await _diffReader.ReadComparisonDiffAsync(repositoryRoot, baseBranch, "HEAD", _options.Value.AI.MaxPromptDiffLines, cancellationToken);
        var recentCommits = await _logReader.ReadRecentCommitsAsync(repositoryRoot, 12, cancellationToken);
        var fallback = _fallbackComposer.BuildPullRequestSummary(baseBranch, diff, recentCommits, language);
        var provider = _aiProviderFactory.Create(providerOverride);
        var health = await provider.CheckHealthAsync(modelOverride, cancellationToken);

        if (!health.IsAvailable)
        {
            return new PullRequestSummaryResult
            {
                Summary = fallback,
                UsedAI = false,
                Warnings = [health.Message]
            };
        }

        var response = await provider.GenerateAsync(
            _summaryPromptBuilder.BuildPullRequestSummary(language, baseBranch, diff, recentCommits, providerOverride, modelOverride),
            cancellationToken);

        var aiSummary = response.Success ? TryParsePullRequestSummary(response.Content) : null;
        return new PullRequestSummaryResult
        {
            Summary = aiSummary ?? fallback,
            UsedAI = aiSummary is not null,
            Warnings = response.Success ? Array.Empty<string>() : [response.ErrorMessage ?? "AI generation failed."]
        };
    }

    private static PullRequestSummary? TryParsePullRequestSummary(string rawContent)
    {
        try
        {
            using var document = JsonDocument.Parse(TextUtilities.ExtractJsonObject(rawContent));
            var root = document.RootElement;
            return new PullRequestSummary
            {
                Summary = root.GetProperty("summary").GetString() ?? string.Empty,
                Changes = root.GetProperty("changes").EnumerateArray().Select(static item => item.GetString() ?? string.Empty).Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray(),
                Motivation = root.GetProperty("motivation").GetString() ?? string.Empty,
                Impact = root.GetProperty("impact").GetString() ?? string.Empty,
                Testing = root.TryGetProperty("testing", out var testingElement) && testingElement.ValueKind != JsonValueKind.Null ? testingElement.GetString() : null
            };
        }
        catch
        {
            return null;
        }
    }
}
