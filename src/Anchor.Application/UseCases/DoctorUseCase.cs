using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Application.UseCases;

public sealed class DoctorUseCase
{
    private readonly IGitRepositoryLocator _repositoryLocator;
    private readonly IRepositoryDoctor _repositoryDoctor;

    public DoctorUseCase(IGitRepositoryLocator repositoryLocator, IRepositoryDoctor repositoryDoctor)
    {
        _repositoryLocator = repositoryLocator;
        _repositoryDoctor = repositoryDoctor;
    }

    public async Task<DoctorReport> ExecuteAsync(string? startPath, CancellationToken cancellationToken)
    {
        var repositoryRoot = await _repositoryLocator.LocateAsync(startPath, cancellationToken);
        return await _repositoryDoctor.AnalyzeAsync(repositoryRoot, cancellationToken);
    }
}
