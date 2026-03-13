using Anchor.Application.Services;
using Anchor.Domain;

namespace Anchor.Application.UseCases;

public sealed class ExplainCommandUseCase
{
    private readonly CommandExplainer _explainer;

    public ExplainCommandUseCase(CommandExplainer explainer)
    {
        _explainer = explainer;
    }

    public CommandExplanation Execute(string commandText, string language) => _explainer.Explain(commandText, language);
}
