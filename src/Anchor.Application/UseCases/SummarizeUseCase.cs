using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Application.Services;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.Application.UseCases;

public sealed class SummarizeUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IGitWorktreeAnalyzer _worktreeAnalyzer;
    private readonly IGitDiffReader _diffReader;
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly ISummaryPromptBuilder _summaryPromptBuilder;
    private readonly SummaryFallbackComposer _fallbackComposer;
    private readonly IOptions<AnchorOptions> _options;

    public SummarizeUseCase(
        IGitRepositoryLocator repositoryLocator,
        IGitWorktreeAnalyzer worktreeAnalyzer,
        IGitDiffReader diffReader,
        IAIProviderFactory aiProviderFactory,
        ISummaryPromptBuilder summaryPromptBuilder,
        SummaryFallbackComposer fallbackComposer,
        IOptions<AnchorOptions> options)
    {
        _repositoryLocator = repositoryLocator;
        _worktreeAnalyzer = worktreeAnalyzer;
        _diffReader = diffReader;
        _aiProviderFactory = aiProviderFactory;
        _summaryPromptBuilder = summaryPromptBuilder;
        _fallbackComposer = fallbackComposer;
        _options = options;
    }

    public async Task<WorkSummaryResult> ExecuteAsync(string? startPath, string language, string? providerOverride, string? modelOverride, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        var repository = await _worktreeAnalyzer.AnalyzeAsync(repositoryRoot, cancellationToken);
        var diff = await _diffReader.ReadAllChangesDiffAsync(repositoryRoot, _options.Value.AI.MaxPromptDiffLines, cancellationToken);
        var fallback = _fallbackComposer.BuildWorkSummary(repository, diff, language);
        var provider = _aiProviderFactory.Create(providerOverride);
        var health = await provider.CheckHealthAsync(modelOverride, cancellationToken);

        if (!health.IsAvailable)
        {
            return fallback;
        }

        var response = await provider.GenerateAsync(
            _summaryPromptBuilder.BuildWorkSummary(language, repository, diff, providerOverride, modelOverride),
            cancellationToken);

        var aiResult = response.Success ? TryParseWorkSummary(response.Content) : null;
        return aiResult ?? fallback;
    }

    private static WorkSummaryResult? TryParseWorkSummary(string rawContent)
    {
        try
        {
            using var document = JsonDocument.Parse(TextUtilities.ExtractJsonObject(rawContent));
            var root = document.RootElement;
            return new WorkSummaryResult
            {
                Summary = root.GetProperty("summary").GetString() ?? string.Empty,
                Highlights = root.GetProperty("highlights").EnumerateArray().Select(static item => item.GetString() ?? string.Empty).Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray(),
                Risks = root.GetProperty("risks").EnumerateArray().Select(static item => item.GetString() ?? string.Empty).Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray(),
                UsedAI = true
            };
        }
        catch
        {
            return null;
        }
    }
}
