using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Application.Services;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.Application.UseCases;

public sealed class BranchAiUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IGitDiffReader _diffReader;
    private readonly CommitIntentAnalyzer _commitIntentAnalyzer;
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly ISummaryPromptBuilder _summaryPromptBuilder;
    private readonly SummaryFallbackComposer _fallbackComposer;
    private readonly IOptions<AnchorOptions> _options;

    public BranchAiUseCase(
        IGitRepositoryLocator repositoryLocator,
        IGitDiffReader diffReader,
        CommitIntentAnalyzer commitIntentAnalyzer,
        IAIProviderFactory aiProviderFactory,
        ISummaryPromptBuilder summaryPromptBuilder,
        SummaryFallbackComposer fallbackComposer,
        IOptions<AnchorOptions> options)
    {
        _repositoryLocator = repositoryLocator;
        _diffReader = diffReader;
        _commitIntentAnalyzer = commitIntentAnalyzer;
        _aiProviderFactory = aiProviderFactory;
        _summaryPromptBuilder = summaryPromptBuilder;
        _fallbackComposer = fallbackComposer;
        _options = options;
    }

    public async Task<BranchNameSuggestion> ExecuteAsync(string? startPath, string? goal, string language, string? providerOverride, string? modelOverride, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        var diff = await _diffReader.ReadAllChangesDiffAsync(repositoryRoot, _options.Value.AI.MaxPromptDiffLines, cancellationToken);
        var analysis = _commitIntentAnalyzer.Analyze(diff.Files, diff.PatchText);
        var fallback = _fallbackComposer.BuildBranchSuggestion(goal ?? string.Empty, analysis, language);
        var provider = _aiProviderFactory.Create(providerOverride);
        var health = await provider.CheckHealthAsync(modelOverride, cancellationToken);

        if (!health.IsAvailable)
        {
            return fallback;
        }

        var response = await provider.GenerateAsync(
            _summaryPromptBuilder.BuildBranchName(language, goal ?? fallback.Name, diff, analysis, providerOverride, modelOverride),
            cancellationToken);

        var aiSuggestion = response.Success ? TryParseBranchName(response.Content) : null;
        return aiSuggestion ?? fallback;
    }

    private static BranchNameSuggestion? TryParseBranchName(string rawContent)
    {
        try
        {
            using var document = JsonDocument.Parse(TextUtilities.ExtractJsonObject(rawContent));
            var root = document.RootElement;
            var name = root.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var parts = name.Split('/', 2, StringSplitOptions.TrimEntries);
            return new BranchNameSuggestion
            {
                Name = name,
                Prefix = parts[0],
                Slug = parts.Length > 1 ? parts[1] : parts[0],
                Confidence = root.TryGetProperty("confidence", out var confidenceElement) && confidenceElement.TryGetInt32(out var confidence)
                    ? confidence
                    : 75,
                Alternatives = root.TryGetProperty("alternatives", out var alternativesElement)
                    ? alternativesElement.EnumerateArray().Select(static item => item.GetString() ?? string.Empty).Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray()
                    : Array.Empty<string>(),
                UsedAI = true
            };
        }
        catch
        {
            return null;
        }
    }
}
