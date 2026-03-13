using System.Linq;
using Anchor.Application.UseCases;
using Anchor.Presentation;
using Spectre.Console.Cli;

namespace Anchor.Cli.Commands;

public sealed class ExplainCommand : AsyncCommand<ExplainCommand.Settings>
{
    public sealed class Settings : AnchorCommandSettings
    {
        [CommandArgument(0, "<COMMAND>")]
        public string[] Tokens { get; init; } = [];
    }

    private readonly DetectUserLanguageUseCase _detectUserLanguageUseCase;
    private readonly ExplainCommandUseCase _useCase;
    private readonly BannerRenderer _bannerRenderer;
    private readonly ExplanationRenderer _renderer;

    public ExplainCommand(
        DetectUserLanguageUseCase detectUserLanguageUseCase,
        ExplainCommandUseCase useCase,
        BannerRenderer bannerRenderer,
        ExplanationRenderer renderer)
    {
        _detectUserLanguageUseCase = detectUserLanguageUseCase;
        _useCase = useCase;
        _bannerRenderer = bannerRenderer;
        _renderer = renderer;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var tokens = GetCommandTokens(context, settings);
        if (tokens.Length == 0)
        {
            return 1;
        }

        var language = await _detectUserLanguageUseCase.ExecuteAsync(settings.Language, CancellationToken.None);
        _bannerRenderer.Render(language.LanguageTag);
        _renderer.Render(_useCase.Execute(string.Join(' ', tokens), language.LanguageTag), language.LanguageTag);
        return 0;
    }

    private static string[] GetCommandTokens(CommandContext context, Settings settings)
    {
        var tokens = context.Arguments.ToList();
        if (tokens.Count > 0 && tokens[0].Equals(context.Name, StringComparison.OrdinalIgnoreCase))
        {
            tokens.RemoveAt(0);
        }

        for (var index = 0; index < tokens.Count; index++)
        {
            if (!tokens[index].Equals("--lang", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            tokens.RemoveAt(index);
            if (index < tokens.Count)
            {
                tokens.RemoveAt(index);
            }

            break;
        }

        return tokens.Count > 0 ? [.. tokens] : settings.Tokens;
    }
}
