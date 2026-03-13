using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Application.Services;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.Application.UseCases;

public sealed class WhyFileUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IGitLogReader _logReader;
    private readonly IGitDiffReader _diffReader;
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly ISummaryPromptBuilder _summaryPromptBuilder;
    private readonly SummaryFallbackComposer _fallbackComposer;
    private readonly IOptions<AnchorOptions> _options;

    public WhyFileUseCase(
        IGitRepositoryLocator repositoryLocator,
        IGitLogReader logReader,
        IGitDiffReader diffReader,
        IAIProviderFactory aiProviderFactory,
        ISummaryPromptBuilder summaryPromptBuilder,
        SummaryFallbackComposer fallbackComposer,
        IOptions<AnchorOptions> options)
    {
        _repositoryLocator = repositoryLocator;
        _logReader = logReader;
        _diffReader = diffReader;
        _aiProviderFactory = aiProviderFactory;
        _summaryPromptBuilder = summaryPromptBuilder;
        _fallbackComposer = fallbackComposer;
        _options = options;
    }

    public async Task<WhyFileResult> ExecuteAsync(string? startPath, string filePath, string language, string? providerOverride, string? modelOverride, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        var history = await _logReader.ReadFileHistoryAsync(repositoryRoot, filePath, 8, cancellationToken);
        var diff = await _diffReader.ReadAllChangesDiffAsync(repositoryRoot, _options.Value.AI.MaxPromptDiffLines, cancellationToken);
        var fallback = _fallbackComposer.BuildWhyFile(filePath, history, language);
        var provider = _aiProviderFactory.Create(providerOverride);
        var health = await provider.CheckHealthAsync(modelOverride, cancellationToken);

        if (!health.IsAvailable)
        {
            return fallback;
        }

        var response = await provider.GenerateAsync(
            _summaryPromptBuilder.BuildWhyFile(language, filePath, diff, history, providerOverride, modelOverride),
            cancellationToken);

        var aiResult = response.Success ? TryParseWhyFile(filePath, response.Content) : null;
        return aiResult ?? fallback;
    }

    private static WhyFileResult? TryParseWhyFile(string filePath, string rawContent)
    {
        try
        {
            using var document = JsonDocument.Parse(TextUtilities.ExtractJsonObject(rawContent));
            var root = document.RootElement;
            return new WhyFileResult
            {
                FilePath = filePath,
                Summary = root.GetProperty("summary").GetString() ?? string.Empty,
                SupportingCommits = root.GetProperty("supportingCommits").EnumerateArray().Select(static item => item.GetString() ?? string.Empty).Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray(),
                UsedAI = true
            };
        }
        catch
        {
            return null;
        }
    }
}
