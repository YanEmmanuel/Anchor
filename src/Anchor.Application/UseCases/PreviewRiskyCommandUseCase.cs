using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Application.UseCases;

public sealed class PreviewRiskyCommandUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IGitStatusReader _statusReader;
    private readonly IRiskAnalyzer _riskAnalyzer;

    public PreviewRiskyCommandUseCase(
        IGitRepositoryLocator repositoryLocator,
        IGitStatusReader statusReader,
        IRiskAnalyzer riskAnalyzer)
    {
        _repositoryLocator = repositoryLocator;
        _statusReader = statusReader;
        _riskAnalyzer = riskAnalyzer;
    }

    public async Task<CommandRiskAnalysis> ExecuteAsync(string? startPath, IReadOnlyList<string> gitArguments, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        var repoState = await _statusReader.ReadAsync(repositoryRoot, cancellationToken);
        var context = new GitCommandContext
        {
            RawCommand = string.Join(' ', gitArguments),
            Tokens = gitArguments,
            RepositoryRoot = repositoryRoot,
            RepoState = repoState
        };

        return await _riskAnalyzer.AnalyzeAsync(context, cancellationToken);
    }
}
