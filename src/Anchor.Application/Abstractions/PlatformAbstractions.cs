using Anchor.Domain;

namespace Anchor.Application.Abstractions;

public interface IAIProvider
{
    AiProviderType ProviderType { get; }
    Task<ProviderHealthStatus> CheckHealthAsync(string? modelOverride, CancellationToken cancellationToken);
    Task<AIResponse> GenerateAsync(AIRequestContext request, CancellationToken cancellationToken);
}

public interface IAIProviderFactory
{
    IAIProvider Create(string? providerOverride = null);
}

public interface ICommitPromptBuilder
{
    AIRequestContext Build(
        CommitGenerationRequest request,
        CommitIntentAnalysis intentAnalysis,
        DiffContent diff,
        IReadOnlyList<GitCommitSummary> recentCommits,
        string commitLanguage);
}

public interface IExplainPromptBuilder
{
    AIRequestContext Build(string commandText, CommandExplanation explanation, string language, string? providerOverride, string? modelOverride);
}

public interface IConflictPromptBuilder
{
    AIRequestContext Build(string repositoryRoot, IReadOnlyList<string> conflictedFiles, string language, string? providerOverride, string? modelOverride);
}

public interface ISummaryPromptBuilder
{
    AIRequestContext BuildPullRequestSummary(
        string language,
        string baseBranch,
        DiffContent diff,
        IReadOnlyList<GitCommitSummary> recentCommits,
        string? providerOverride,
        string? modelOverride);

    AIRequestContext BuildWorkSummary(
        string language,
        GitRepoContext repository,
        DiffContent diff,
        string? providerOverride,
        string? modelOverride);

    AIRequestContext BuildWhyFile(
        string language,
        string filePath,
        DiffContent diff,
        IReadOnlyList<GitCommitSummary> fileHistory,
        string? providerOverride,
        string? modelOverride);

    AIRequestContext BuildBranchName(
        string language,
        string goal,
        DiffContent diff,
        CommitIntentAnalysis analysis,
        string? providerOverride,
        string? modelOverride);
}

public interface IUserLanguageResolver
{
    ValueTask<UserLanguageContext> ResolveAsync(string? commandLineOverride, CancellationToken cancellationToken);
}

public interface ILocalizer
{
    string Get(string key, string? language = null, params object[] arguments);
}

public interface ISessionState
{
    UserLanguageContext? LanguageContext { get; set; }
}

public interface IAppPathProvider
{
    string GetConfigDirectory();
    string GetConfigFilePath();
    string GetSnapshotDirectory(string repositoryRoot);
    string GetRepositoryId(string repositoryRoot);
}

public interface IClipboardService
{
    Task<bool> CopyAsync(string text, CancellationToken cancellationToken);
}

public interface IEnvironmentReader
{
    string? GetEnvironmentVariable(string name);
}

public interface ISnapshotService
{
    Task<Snapshot> CreateAsync(string repositoryRoot, string description, CancellationToken cancellationToken);
}

public interface IRecoveryService
{
    Task<IReadOnlyList<RecoveryPoint>> ListAsync(string repositoryRoot, CancellationToken cancellationToken);
    Task<GitCommandResult> RestoreAsync(string repositoryRoot, string snapshotId, CancellationToken cancellationToken);
}

public interface IRepositoryDoctor
{
    Task<DoctorReport> AnalyzeAsync(string repositoryRoot, CancellationToken cancellationToken);
}

public interface IRiskAnalyzer
{
    Task<CommandRiskAnalysis> AnalyzeAsync(GitCommandContext context, CancellationToken cancellationToken);
}
