using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace HeroStory.Infrastructure.Clients;

public class OpenAiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenAiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.BaseAddress ??= new Uri("https://api.openai.com");
        var apiKey = _configuration["OPENAI_API_KEY"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(apiKey) && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        var timeoutSeconds = int.TryParse(_configuration["OPENAI_REQUEST_TIMEOUT_SECONDS"], out var parsedTimeout)
            ? parsedTimeout
            : 30;
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<string> CreateChatCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        var model = _configuration["OPENAI_TEXT_MODEL"] ?? "gpt-4o";
        var maxTokens = int.TryParse(_configuration["OPENAI_TEXT_MAX_TOKENS"], out var parsedMaxTokens) ? parsedMaxTokens : 1400;
        var temperature = decimal.TryParse(_configuration["OPENAI_TEXT_TEMPERATURE"], out var parsedTemperature) ? parsedTemperature : 0.85m;

        var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", new
        {
            model,
            max_tokens = maxTokens,
            temperature,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = "You are a collaborative fantasy storyteller." },
                new { role = "user", content = prompt }
            }
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("OpenAI chat response was empty.");
        return payload.Choices.FirstOrDefault()?.Message.Content?.Trim()
            ?? throw new InvalidOperationException("OpenAI chat response did not include a message.");
    }

    public async Task<bool> IsFlaggedAsync(string input, CancellationToken cancellationToken)
    {
        var model = _configuration["OPENAI_MODERATION_MODEL"] ?? "text-moderation-latest";
        var response = await _httpClient.PostAsJsonAsync("/v1/moderations", new { model, input }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ModerationResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("OpenAI moderation response was empty.");
        return payload.Results.FirstOrDefault()?.Flagged ?? false;
    }

    private sealed record ChatResponse(IReadOnlyList<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
    private sealed record ModerationResponse(IReadOnlyList<ModerationResult> Results);
    private sealed record ModerationResult(bool Flagged);
}
