using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Application.UseCases;

public sealed class SnapshotUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly ISnapshotService _snapshotService;

    public SnapshotUseCase(IGitRepositoryLocator repositoryLocator, ISnapshotService snapshotService)
    {
        _repositoryLocator = repositoryLocator;
        _snapshotService = snapshotService;
    }

    public async Task<Snapshot> ExecuteAsync(string? startPath, string description, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        return await _snapshotService.CreateAsync(repositoryRoot, description, cancellationToken);
    }
}
