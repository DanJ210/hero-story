using HeroStory.Api.DTOs.Scene;
using HeroStory.Infrastructure.Clients;

namespace HeroStory.Api.Services;

public class OpenAiTextService : IOpenAiTextService
{
    private readonly OpenAiClient _openAiClient;
    private readonly ILogger<OpenAiTextService> _logger;
    private readonly int _maxRetries;
    private readonly int _retryDelayMs;

    public OpenAiTextService(OpenAiClient openAiClient, IConfiguration configuration, ILogger<OpenAiTextService> logger)
    {
        _openAiClient = openAiClient;
        _logger = logger;
        _maxRetries = Math.Clamp(configuration.GetValue("OPENAI_TEXT_MAX_RETRIES", 2), 0, 5);
        _retryDelayMs = Math.Clamp(configuration.GetValue("OPENAI_TEXT_RETRY_DELAY_MS", 250), 0, 5000);
    }

    public async Task<GeneratedStoryTurn> GenerateTurnAsync(string prompt, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var response = await _openAiClient.CreateChatCompletionAsync(
                attempt == 0 ? prompt : $"{prompt}\n\nPrevious output was invalid. Return only valid JSON matching every required field and constraint.",
                cancellationToken);
            try
            {
                return StoryTurnResponseParser.Parse(response);
            }
            catch (InvalidOperationException exception) when (attempt < _maxRetries)
            {
                _logger.LogWarning(
                    exception,
                    "Structured story turn validation failed on attempt {Attempt}; retrying. RetryLimit={RetryLimit}",
                    attempt + 1,
                    _maxRetries);
                if (_retryDelayMs > 0)
                {
                    await Task.Delay(_retryDelayMs, cancellationToken);
                }
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(
                    exception,
                    "Structured story turn validation failed after {Attempts} attempts.",
                    attempt + 1);
                throw;
            }
        }
    }
}
