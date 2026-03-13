using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Application.Configuration;
using Anchor.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Anchor.AI;

public sealed class AnthropicCompatibleProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AnchorOptions> _options;
    private readonly ILogger<AnthropicCompatibleProvider> _logger;

    public AnthropicCompatibleProvider(IHttpClientFactory httpClientFactory, IOptions<AnchorOptions> options, ILogger<AnthropicCompatibleProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public AiProviderType ProviderType => AiProviderType.AnthropicCompatible;

    public Task<ProviderHealthStatus> CheckHealthAsync(string? modelOverride, CancellationToken cancellationToken)
    {
        var settings = _options.Value.AI.AnthropicCompatible;
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
                ? "Anthropic-compatible provider is configured."
                : "Anthropic-compatible provider is disabled or incomplete."
        });
    }

    public async Task<AIResponse> GenerateAsync(AIRequestContext request, CancellationToken cancellationToken)
    {
        var settings = _options.Value.AI.AnthropicCompatible;
        var model = string.IsNullOrWhiteSpace(request.ModelOverride) ? settings.Model : request.ModelOverride;
        var client = CreateClient(settings.BaseUrl, settings.ApiKey, request.Timeout ?? TimeSpan.FromSeconds(_options.Value.AI.TimeoutSeconds));

        try
        {
            using var response = await client.PostAsJsonAsync(
                "/messages",
                new
                {
                    model,
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature,
                    system = request.SystemPrompt,
                    messages = new object[]
                    {
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
                    ProviderName = "anthropic-compatible",
                    Model = model ?? string.Empty,
                    RawResponse = raw,
                    ErrorMessage = raw
                };
            }

            using var document = JsonDocument.Parse(raw);
            var content = document.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            return new AIResponse
            {
                Success = !string.IsNullOrWhiteSpace(content),
                Content = content,
                ProviderName = "anthropic-compatible",
                Model = model ?? string.Empty,
                RawResponse = raw,
                ErrorMessage = string.IsNullOrWhiteSpace(content) ? "Provider returned an empty response." : null
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Anthropic-compatible generation failed.");
            return new AIResponse
            {
                Success = false,
                ProviderName = "anthropic-compatible",
                Model = model ?? string.Empty,
                ErrorMessage = exception.Message
            };
        }
    }

    private HttpClient CreateClient(string baseUrl, string apiKey, TimeSpan timeout)
    {
        var client = _httpClientFactory.CreateClient(nameof(AnthropicCompatibleProvider));
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = timeout;
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}
