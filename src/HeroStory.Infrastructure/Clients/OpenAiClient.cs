using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace HeroStory.Infrastructure.Clients;

public class OpenAiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _requestTimeout;

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
        _requestTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
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

    public const string UnspecifiedModerationCategory = "unspecified";

    public virtual async Task<IReadOnlyList<string>> GetFlaggedCategoriesAsync(string input, CancellationToken cancellationToken)
    {
        var model = _configuration["OPENAI_MODERATION_MODEL"] ?? "omni-moderation-latest";
        var response = await _httpClient.PostAsJsonAsync("/v1/moderations", new { model, input }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ModerationResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("OpenAI moderation response was empty.");
        var result = payload.Results.FirstOrDefault();
        if (result is null || !result.Flagged)
        {
            return [];
        }

        var flagged = result.Categories?.Where(category => category.Value).Select(category => category.Key).ToArray() ?? [];
        // A flagged result without categories must still block.
        return flagged.Length == 0 ? [UnspecifiedModerationCategory] : flagged;
    }

    public async Task<byte[]> GenerateImageAsync(string imagePrompt, CancellationToken cancellationToken)
    {
        var model = _configuration["OPENAI_IMAGE_MODEL"] ?? "gpt-image-1";
        var size = _configuration["OPENAI_IMAGE_SIZE"] ?? "1024x1024";
        var quality = _configuration["OPENAI_IMAGE_QUALITY"] ?? "auto";
        var boundedPrompt = imagePrompt.Length > 4000 ? imagePrompt[..4000] : imagePrompt;
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(_requestTimeout);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/v1/images/generations", new
            {
                model,
                prompt = boundedPrompt,
                n = 1,
                size,
                quality,
                output_format = "png"
            }, timeoutCancellation.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"OpenAI image generation did not respond within {_requestTimeout.TotalSeconds:0} seconds.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(timeoutCancellation.Token);
            throw new HttpRequestException($"OpenAI image generation failed with {(int)response.StatusCode} ({response.StatusCode}): {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ImageGenerationResponse>(cancellationToken: timeoutCancellation.Token)
            ?? throw new InvalidOperationException("OpenAI image generation response was empty.");
        var image = payload.Data.FirstOrDefault()
            ?? throw new InvalidOperationException("OpenAI image generation response did not include an image.");

        if (!string.IsNullOrWhiteSpace(image.B64Json))
        {
            return Convert.FromBase64String(image.B64Json);
        }

        if (!string.IsNullOrWhiteSpace(image.Url))
        {
            return await _httpClient.GetByteArrayAsync(image.Url, timeoutCancellation.Token);
        }

        throw new InvalidOperationException("OpenAI image generation response did not include image data.");
    }

    public async Task<byte[]> GenerateImageWithReferenceAsync(string imagePrompt, Stream referenceImage, string contentType, CancellationToken cancellationToken)
    {
        var model = _configuration["OPENAI_IMAGE_MODEL"] ?? "gpt-image-1";
        var size = _configuration["OPENAI_IMAGE_SIZE"] ?? "1024x1024";
        var quality = _configuration["OPENAI_IMAGE_QUALITY"] ?? "auto";
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(_requestTimeout);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(model), "model");
        form.Add(new StringContent(imagePrompt), "prompt");
        form.Add(new StringContent("1"), "n");
        form.Add(new StringContent(size), "size");
        form.Add(new StringContent(quality), "quality");
        form.Add(new StringContent("png"), "output_format");
        var imageContent = new StreamContent(referenceImage);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(imageContent, "image", "portrait");

        var response = await _httpClient.PostAsync("/v1/images/edits", form, timeoutCancellation.Token);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(timeoutCancellation.Token);
            throw new HttpRequestException($"OpenAI image edit failed with {(int)response.StatusCode} ({response.StatusCode}): {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ImageGenerationResponse>(cancellationToken: timeoutCancellation.Token)
            ?? throw new InvalidOperationException("OpenAI image edit response was empty.");
        var image = payload.Data.FirstOrDefault()
            ?? throw new InvalidOperationException("OpenAI image edit response did not include an image.");
        if (!string.IsNullOrWhiteSpace(image.B64Json))
        {
            return Convert.FromBase64String(image.B64Json);
        }
        if (!string.IsNullOrWhiteSpace(image.Url))
        {
            return await _httpClient.GetByteArrayAsync(image.Url, timeoutCancellation.Token);
        }
        throw new InvalidOperationException("OpenAI image edit response did not include image data.");
    }

    private sealed record ChatResponse(IReadOnlyList<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
    private sealed record ChatMessage(string Content);
    private sealed record ModerationResponse(IReadOnlyList<ModerationResult> Results);
    private sealed record ModerationResult(bool Flagged, IReadOnlyDictionary<string, bool>? Categories);
    private sealed class ImageGenerationResponse
    {
        [JsonPropertyName("data")]
        public IReadOnlyList<ImageData> Data { get; init; } = [];
    }

    private sealed class ImageData
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("b64_json")]
        public string? B64Json { get; init; }
    }
}
