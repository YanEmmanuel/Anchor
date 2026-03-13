using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Infrastructure;

public sealed class SessionState : ISessionState
{
    public UserLanguageContext? LanguageContext { get; set; }
}
