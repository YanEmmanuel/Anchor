using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Application.Services;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.Application.UseCases;

public sealed class CommitAiUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IGitDiffReader _diffReader;
    private readonly IGitLogReader _logReader;
    private readonly CommitIntentAnalyzer _commitIntentAnalyzer;
    private readonly CommitMessageFallbackGenerator _fallbackGenerator;
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly ICommitPromptBuilder _commitPromptBuilder;
    private readonly IOptions<AnchorOptions> _options;
    private readonly ILocalizer _localizer;

    public CommitAiUseCase(
        IGitRepositoryLocator repositoryLocator,
        IGitDiffReader diffReader,
        IGitLogReader logReader,
        CommitIntentAnalyzer commitIntentAnalyzer,
        CommitMessageFallbackGenerator fallbackGenerator,
        IAIProviderFactory aiProviderFactory,
        ICommitPromptBuilder commitPromptBuilder,
        IOptions<AnchorOptions> options,
        ILocalizer localizer)
    {
        _repositoryLocator = repositoryLocator;
        _diffReader = diffReader;
        _logReader = logReader;
        _commitIntentAnalyzer = commitIntentAnalyzer;
        _fallbackGenerator = fallbackGenerator;
        _aiProviderFactory = aiProviderFactory;
        _commitPromptBuilder = commitPromptBuilder;
        _options = options;
        _localizer = localizer;
    }

    public async Task<CommitGenerationResult> ExecuteAsync(CommitGenerationRequest request, UserLanguageContext userLanguage, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(request.RepositoryRoot, cancellationToken);
        var diff = request.IncludeAllChanges
            ? await _diffReader.ReadAllChangesDiffAsync(repositoryRoot, _options.Value.AI.MaxPromptDiffLines, cancellationToken)
            : await _diffReader.ReadStagedDiffAsync(repositoryRoot, _options.Value.AI.MaxPromptDiffLines, cancellationToken);

        if (diff.Files.Count == 0 || string.IsNullOrWhiteSpace(diff.PatchText))
        {
            throw new InvalidOperationException(_localizer.Get("CommitAi.NoChanges", userLanguage.LanguageTag));
        }

        var recentCommits = await _logReader.ReadRecentCommitsAsync(repositoryRoot, 10, cancellationToken);
        var intentAnalysis = _commitIntentAnalyzer.Analyze(diff.Files, diff.PatchText);
        var commitLanguage = _fallbackGenerator.ResolveCommitLanguage(userLanguage, _options.Value);
        var fallbackSuggestion = _fallbackGenerator.Generate(intentAnalysis, diff, commitLanguage);
        var warnings = new List<string>();

        if (!request.IncludeBody)
        {
            fallbackSuggestion = fallbackSuggestion with { Body = null };
        }

        var provider = _aiProviderFactory.Create(request.ProviderOverride);
        var health = await provider.CheckHealthAsync(request.ModelOverride, cancellationToken);
        if (!health.IsAvailable)
        {
            warnings.Add(health.Message);
            return new CommitGenerationResult
            {
                PrimarySuggestion = fallbackSuggestion,
                Alternatives = Array.Empty<CommitSuggestion>(),
                IntentAnalysis = intentAnalysis,
                UsedAI = false,
                ProviderName = provider.ProviderType.ToString(),
                Model = health.Model ?? "deterministic",
                Warnings = warnings
            };
        }

        var prompt = _commitPromptBuilder.Build(request, intentAnalysis, diff, recentCommits, commitLanguage);
        var response = await provider.GenerateAsync(prompt, cancellationToken);
        if (!response.Success)
        {
            warnings.Add(response.ErrorMessage ?? "AI generation failed.");
            return new CommitGenerationResult
            {
                PrimarySuggestion = fallbackSuggestion,
                Alternatives = Array.Empty<CommitSuggestion>(),
                IntentAnalysis = intentAnalysis,
                UsedAI = false,
                ProviderName = response.ProviderName,
                Model = response.Model,
                Warnings = warnings
            };
        }

        var aiSuggestion = TryParseCommitSuggestion(response.Content, request.IncludeBody) ?? fallbackSuggestion;
        if (aiSuggestion == fallbackSuggestion)
        {
            warnings.Add("AI response could not be parsed; deterministic suggestion was used.");
        }

        return new CommitGenerationResult
        {
            PrimarySuggestion = aiSuggestion,
            Alternatives = aiSuggestion == fallbackSuggestion ? Array.Empty<CommitSuggestion>() : [fallbackSuggestion],
            IntentAnalysis = intentAnalysis,
            UsedAI = aiSuggestion != fallbackSuggestion,
            ProviderName = response.ProviderName,
            Model = response.Model,
            Warnings = warnings
        };
    }

    private static CommitSuggestion? TryParseCommitSuggestion(string rawContent, bool includeBody)
    {
        try
        {
            using var document = JsonDocument.Parse(TextUtilities.ExtractJsonObject(rawContent));
            var root = document.RootElement;
            var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            var title = root.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;

            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return new CommitSuggestion
            {
                Type = type!,
                Scope = root.TryGetProperty("scope", out var scopeElement) && scopeElement.ValueKind != JsonValueKind.Null ? scopeElement.GetString() : null,
                Title = title!,
                Body = includeBody && root.TryGetProperty("body", out var bodyElement) && bodyElement.ValueKind != JsonValueKind.Null ? bodyElement.GetString() : null,
                IsBreakingChange = root.TryGetProperty("breakingChange", out var breakingElement) && breakingElement.GetBoolean(),
                BreakingChangeDescription = root.TryGetProperty("breakingChangeDescription", out var breakingDescriptionElement) && breakingDescriptionElement.ValueKind != JsonValueKind.Null
                    ? breakingDescriptionElement.GetString()
                    : null,
                Highlights = root.TryGetProperty("highlights", out var highlightsElement) && highlightsElement.ValueKind == JsonValueKind.Array
                    ? highlightsElement.EnumerateArray().Select(static item => item.GetString() ?? string.Empty).Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray()
                    : Array.Empty<string>(),
                Confidence = root.TryGetProperty("confidence", out var confidenceElement) && confidenceElement.TryGetInt32(out var confidence)
                    ? confidence
                    : 75
            };
        }
        catch
        {
            return null;
        }
    }
}
