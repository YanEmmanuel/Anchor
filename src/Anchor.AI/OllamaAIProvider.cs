using System.Net.Http.Json;
using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Anchor.AI;

public sealed class OllamaAIProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AnchorOptions> _options;
    private readonly ILogger<OllamaAIProvider> _logger;

    public OllamaAIProvider(IHttpClientFactory httpClientFactory, IOptions<AnchorOptions> options, ILogger<OllamaAIProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public AiProviderType ProviderType => AiProviderType.Ollama;

    public async Task<ProviderHealthStatus> CheckHealthAsync(string? modelOverride, CancellationToken cancellationToken)
    {
        var settings = _options.Value.AI.Ollama;
        var model = string.IsNullOrWhiteSpace(modelOverride) ? settings.Model : modelOverride;
        var client = CreateClient(settings.BaseUrl);

        try
        {
            using var response = await client.GetAsync("/api/tags", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new ProviderHealthStatus
                {
                    ProviderType = ProviderType,
                    IsAvailable = false,
                    Model = model,
                    Message = $"Ollama is unreachable at {settings.BaseUrl}."
                };
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var modelFound = document.RootElement
                .GetProperty("models")
                .EnumerateArray()
                .Any(element => element.TryGetProperty("name", out var name)
                                && name.GetString()?.Equals(model, StringComparison.OrdinalIgnoreCase) == true);

            return new ProviderHealthStatus
            {
                ProviderType = ProviderType,
                IsAvailable = modelFound,
                Model = model,
                Message = modelFound
                    ? "Ollama is available."
                    : $"Model '{model}' was not found. Run: ollama pull {model}"
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to reach Ollama.");
            return new ProviderHealthStatus
            {
                ProviderType = ProviderType,
                IsAvailable = false,
                Model = model,
                Message = $"Unable to reach Ollama at {settings.BaseUrl}: {exception.Message}"
            };
        }
    }

    public async Task<AIResponse> GenerateAsync(AIRequestContext request, CancellationToken cancellationToken)
    {
        var settings = _options.Value.AI.Ollama;
        var model = string.IsNullOrWhiteSpace(request.ModelOverride) ? settings.Model : request.ModelOverride;
        var client = CreateClient(settings.BaseUrl, request.Timeout ?? TimeSpan.FromSeconds(_options.Value.AI.TimeoutSeconds));

        var prompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}";

        try
        {
            using var response = await client.PostAsJsonAsync(
                "/api/generate",
                new
                {
                    model,
                    prompt,
                    stream = false,
                    options = new
                    {
                        temperature = request.Temperature
                    }
                },
                cancellationToken);

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AIResponse
                {
                    Success = false,
                    ProviderName = "ollama",
                    Model = model ?? string.Empty,
                    RawResponse = raw,
                    ErrorMessage = raw
                };
            }

            using var document = JsonDocument.Parse(raw);
            var content = document.RootElement.TryGetProperty("response", out var responseElement)
                ? responseElement.GetString() ?? string.Empty
                : string.Empty;

            return new AIResponse
            {
                Success = !string.IsNullOrWhiteSpace(content),
                Content = content,
                ProviderName = "ollama",
                Model = model ?? string.Empty,
                RawResponse = raw,
                ErrorMessage = string.IsNullOrWhiteSpace(content) ? "Ollama returned an empty response." : null
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Ollama generation failed.");
            return new AIResponse
            {
                Success = false,
                ProviderName = "ollama",
                Model = model ?? string.Empty,
                ErrorMessage = exception.Message
            };
        }
    }

    private HttpClient CreateClient(string baseUrl, TimeSpan? timeout = null)
    {
        var client = _httpClientFactory.CreateClient(nameof(OllamaAIProvider));
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = timeout ?? TimeSpan.FromSeconds(_options.Value.AI.TimeoutSeconds);
        return client;
    }
}
