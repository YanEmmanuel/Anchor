using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Anchor.AI;

public sealed class OpenAICompatibleProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AnchorOptions> _options;
    private readonly ILogger<OpenAICompatibleProvider> _logger;

    public OpenAICompatibleProvider(IHttpClientFactory httpClientFactory, IOptions<AnchorOptions> options, ILogger<OpenAICompatibleProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public AiProviderType ProviderType => AiProviderType.OpenAiCompatible;

    public Task<ProviderHealthStatus> CheckHealthAsync(string? modelOverride, CancellationToken cancellationToken)
    {
        var settings = _options.Value.AI.OpenAICompatible;
        var model = string.IsNullOrWhiteSpace(modelOverride) ? settings.Model : modelOverride;
        var available = settings.Enabled
                        && !string.IsNullOrWhiteSpace(settings.BaseUrl)
                        && !string.IsNullOrWhiteSpace(settings.ApiKey)
                        && !string.IsNullOrWhiteSpace(model);

        return Task.FromResult(new ProviderHealthStatus
        {
            ProviderType = ProviderType,
            IsAvailable = available,
            Model = model,
            Message = available
                ? "OpenAI-compatible provider is configured."
                : "OpenAI-compatible provider is disabled or incomplete."
        });
    }

    public async Task<AIResponse> GenerateAsync(AIRequestContext request, CancellationToken cancellationToken)
    {
        var settings = _options.Value.AI.OpenAICompatible;
        var model = string.IsNullOrWhiteSpace(request.ModelOverride) ? settings.Model : request.ModelOverride;
        var client = CreateClient(settings.BaseUrl, settings.ApiKey, request.Timeout ?? TimeSpan.FromSeconds(_options.Value.AI.TimeoutSeconds));

        try
        {
            using var response = await client.PostAsJsonAsync(
                "/chat/completions",
                new
                {
                    model,
                    temperature = request.Temperature,
                    messages = new object[]
                    {
                        new { role = "system", content = request.SystemPrompt },
                        new { role = "user", content = request.UserPrompt }
                    }
                },
                cancellationToken);

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AIResponse
                {
                    Success = false,
                    ProviderName = "openai-compatible",
                    Model = model ?? string.Empty,
                    RawResponse = raw,
                    ErrorMessage = raw
                };
            }

            using var document = JsonDocument.Parse(raw);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return new AIResponse
            {
                Success = !string.IsNullOrWhiteSpace(content),
                Content = content,
                ProviderName = "openai-compatible",
                Model = model ?? string.Empty,
                RawResponse = raw,
                ErrorMessage = string.IsNullOrWhiteSpace(content) ? "Provider returned an empty response." : null
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "OpenAI-compatible generation failed.");
            return new AIResponse
            {
                Success = false,
                ProviderName = "openai-compatible",
                Model = model ?? string.Empty,
                ErrorMessage = exception.Message
            };
        }
    }

    private HttpClient CreateClient(string baseUrl, string apiKey, TimeSpan timeout)
    {
        var client = _httpClientFactory.CreateClient(nameof(OpenAICompatibleProvider));
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = timeout;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }
}
