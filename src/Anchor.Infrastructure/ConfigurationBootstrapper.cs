using System.Text.Json;
using System.Text.Json.Serialization;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Domain;

namespace Anchor.Infrastructure;

public sealed class ConfigurationBootstrapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly IAppPathProvider _appPathProvider;

    public ConfigurationBootstrapper(IAppPathProvider appPathProvider)
    {
        _appPathProvider = appPathProvider;
    }

    public async Task EnsureConfigurationFileAsync(CancellationToken cancellationToken)
    {
        var configDirectory = _appPathProvider.GetConfigDirectory();
        Directory.CreateDirectory(configDirectory);

        var configFilePath = _appPathProvider.GetConfigFilePath();
        if (File.Exists(configFilePath))
        {
            return;
        }

        var options = new AnchorOptions
        {
            Language = "auto",
            GitExecutablePath = "git",
            CommitLanguageMode = CommitLanguageMode.User,
            AI = new AIOptions
            {
                DefaultProvider = AiProviderType.Ollama,
                Ollama = new LocalModelOptions
                {
                    BaseUrl = "http://localhost:11434",
                    Model = "qwen2.5:14b-instruct"
                },
                OpenAICompatible = new ExternalModelOptions(),
                AnthropicCompatible = new ExternalModelOptions()
            },
            Safety = new SafetyOptions
            {
                AutoSnapshotBeforeDangerousCommands = true,
                ConfirmationLevel = "strict"
            }
        };

        var json = JsonSerializer.Serialize(options, SerializerOptions);

        await File.WriteAllTextAsync(configFilePath, json, cancellationToken);
    }
}
