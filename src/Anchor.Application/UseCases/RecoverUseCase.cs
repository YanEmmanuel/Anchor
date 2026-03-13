using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Application.UseCases;

public sealed class RecoverUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IRecoveryService _recoveryService;

    public RecoverUseCase(IGitRepositoryLocator repositoryLocator, IRecoveryService recoveryService)
    {
        _repositoryLocator = repositoryLocator;
        _recoveryService = recoveryService;
    }

    public async Task<IReadOnlyList<RecoveryPoint>> ListAsync(string? startPath, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        return await _recoveryService.ListAsync(repositoryRoot, cancellationToken);
    }

    public async Task<GitCommandResult> RestoreAsync(string? startPath, string snapshotId, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        return await _recoveryService.RestoreAsync(repositoryRoot, snapshotId, cancellationToken);
    }
}
