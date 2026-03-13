using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Application.UseCases;

public sealed class DetectUserLanguageUseCase
{
    private readonly IUserLanguageResolver _resolver;
    private readonly ISessionState _sessionState;

    public DetectUserLanguageUseCase(IUserLanguageResolver resolver, ISessionState sessionState)
    {
        _resolver = resolver;
        _sessionState = sessionState;
    }

    public async ValueTask<UserLanguageContext> ExecuteAsync(string? commandLineLanguage, CancellationToken cancellationToken)
    {
        if (_sessionState.LanguageContext is not null
            && (string.IsNullOrWhiteSpace(commandLineLanguage)
                || _sessionState.LanguageContext.LanguageTag.Equals(commandLineLanguage, StringComparison.OrdinalIgnoreCase)))
        {
            return _sessionState.LanguageContext;
        }

        var resolved = await _resolver.ResolveAsync(commandLineLanguage, cancellationToken);
        _sessionState.LanguageContext = resolved;
        return resolved;
    }
}
