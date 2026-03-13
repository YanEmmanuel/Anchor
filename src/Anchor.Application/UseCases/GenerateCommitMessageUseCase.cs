using Anchor.Domain;

namespace Anchor.Application.UseCases;

public sealed class GenerateCommitMessageUseCase
{
    private readonly CommitAiUseCase _commitAiUseCase;

    public GenerateCommitMessageUseCase(CommitAiUseCase commitAiUseCase)
    {
        _commitAiUseCase = commitAiUseCase;
    }

    public Task<CommitGenerationResult> ExecuteAsync(CommitGenerationRequest request, UserLanguageContext userLanguage, CancellationToken cancellationToken) =>
        _commitAiUseCase.ExecuteAsync(request, userLanguage, cancellationToken);
}
