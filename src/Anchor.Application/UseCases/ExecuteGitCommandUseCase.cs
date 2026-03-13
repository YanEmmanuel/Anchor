using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Domain;
using Microsoft.Extensions.Options;

namespace Anchor.Application.UseCases;

public sealed class ExecuteGitCommandUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IGitStatusReader _statusReader;
    private readonly IRiskAnalyzer _riskAnalyzer;
    private readonly ISnapshotService _snapshotService;
    private readonly IGitCommandExecutor _gitCommandExecutor;
    private readonly IOptions<AnchorOptions> _options;

    public ExecuteGitCommandUseCase(
        IGitRepositoryLocator repositoryLocator,
        IGitStatusReader statusReader,
        IRiskAnalyzer riskAnalyzer,
        ISnapshotService snapshotService,
        IGitCommandExecutor gitCommandExecutor,
        IOptions<AnchorOptions> options)
    {
        _repositoryLocator = repositoryLocator;
        _statusReader = statusReader;
        _riskAnalyzer = riskAnalyzer;
        _snapshotService = snapshotService;
        _gitCommandExecutor = gitCommandExecutor;
        _options = options;
    }

    public async Task<GitPassthroughResult> ExecuteAsync(string? startPath, IReadOnlyList<string> gitArguments, bool bypassConfirmation, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        var repoState = await _statusReader.ReadAsync(repositoryRoot, cancellationToken);
        var risk = await _riskAnalyzer.AnalyzeAsync(
            new GitCommandContext
            {
                RawCommand = string.Join(' ', gitArguments),
                Tokens = gitArguments,
                RepositoryRoot = repositoryRoot,
                RepoState = repoState
            },
            cancellationToken);

        string? snapshotId = null;
        if (risk.ShouldCreateSnapshot && _options.Value.Safety.AutoSnapshotBeforeDangerousCommands)
        {
            var snapshot = await _snapshotService.CreateAsync(repositoryRoot, $"Before running: git {string.Join(' ', gitArguments)}", cancellationToken);
            snapshotId = snapshot.Id;
        }

        if (risk.RequiresConfirmation && !bypassConfirmation)
        {
            return new GitPassthroughResult(repositoryRoot, risk, snapshotId, new GitCommandResult(130, string.Empty, "Execution cancelled by user."));
        }

        var result = await _gitCommandExecutor.ExecuteAsync(repositoryRoot, gitArguments, cancellationToken);
        return new GitPassthroughResult(repositoryRoot, risk, snapshotId, result);
    }
}

public sealed record GitPassthroughResult(
    string RepositoryRoot,
    CommandRiskAnalysis RiskAnalysis,
    string? SnapshotId,
    GitCommandResult CommandResult);
