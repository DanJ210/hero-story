using System.Text.Json;
using HeroStory.Api.DTOs.Scene;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Api.Services;

public class SceneService : ISceneService
{
    private readonly AppDbContext _dbContext;
    private readonly IModerationService _moderationService;
    private readonly IOpenAiTextService _openAiTextService;
    private readonly AzureQueueClient _queueClient;

    public SceneService(
        AppDbContext dbContext,
        IModerationService moderationService,
        IOpenAiTextService openAiTextService,
        AzureQueueClient queueClient)
    {
        _dbContext = dbContext;
        _moderationService = moderationService;
        _openAiTextService = openAiTextService;
        _queueClient = queueClient;
    }

    public async Task<IReadOnlyList<SceneListDto>> GetScenesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
        => await _dbContext.Scenes
            .Where(x => x.SessionId == sessionId && x.Session.UserId == userId)
            .OrderBy(x => x.SequenceNumber)
            .Select(x => new SceneListDto(x.Id, x.SequenceNumber, x.ChoiceText, x.ImageUrl, x.ModerationStatus, x.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<SceneDto?> GetSceneAsync(Guid userId, Guid sessionId, Guid sceneId, CancellationToken cancellationToken)
    {
        var scene = await _dbContext.Scenes
            .AsNoTracking()
            .Where(x => x.Id == sceneId && x.SessionId == sessionId && x.Session.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken);

        return scene is null ? null : ToDto(scene);
    }

    public async Task<SceneDto> CreateSceneAsync(Guid userId, Guid sessionId, CreateSceneRequest request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.StorySessions.Include(x => x.Scenes).SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Session not found.");

        var moderation = await _moderationService.ModerateInputAsync(request.ChoiceText, cancellationToken);
        if (moderation.Status != ModerationStatus.Approved)
        {
            session.ModerationFailureCount += 1;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(moderation.Detail ?? "Scene input was rejected.");
        }

        var prompt = BuildPrompt(session, request.ChoiceText);
        var generatedTurn = await _openAiTextService.GenerateTurnAsync(prompt, cancellationToken);
        var outputModeration = await _moderationService.ModerateOutputAsync(generatedTurn.NarrativeText, cancellationToken);

        var sequenceNumber = (await _dbContext.Scenes
            .Where(x => x.SessionId == sessionId)
            .Select(x => (int?)x.SequenceNumber)
            .MaxAsync(cancellationToken) ?? 0) + 1;
        var scene = new Scene
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SequenceNumber = sequenceNumber,
            ChoiceText = request.ChoiceText,
            NarrativeText = outputModeration.Narrative,
            SceneSummary = generatedTurn.SceneSummary,
            Location = generatedTurn.Location,
            ActiveConflict = generatedTurn.ActiveConflict,
            StoryStateSchemaVersion = 1,
            StoryStateJson = generatedTurn.StoryStateJson,
            SuggestedActionsJson = JsonSerializer.Serialize(generatedTurn.SuggestedActions),
            StoryBeat = generatedTurn.StoryBeat,
            IsEpisodeComplete = generatedTurn.IsEpisodeComplete,
            ModerationStatus = outputModeration.Status,
            ModerationDetail = outputModeration.Detail,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var job = new GenerationJob
        {
            Id = Guid.NewGuid(),
            SceneId = scene.Id,
            SessionId = sessionId,
            Prompt = prompt,
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Scenes.Add(scene);
        _dbContext.GenerationJobs.Add(job);
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = JsonSerializer.Serialize(new { jobId = job.Id, sceneId = scene.Id, sessionId });
        await _queueClient.EnqueueAsync(message, cancellationToken);

                return ToDto(scene);
    }

    private static string BuildPrompt(StorySession session, string choiceText)
                => $$"""
                        Continue an interactive superhero story in which the user is the protagonist.
                        Title: {{session.Title}}
                        Genre: {{session.Genre}}
                        Hero archetype: {{session.HeroArchetype}}
                        Hero name: {{session.HeroName}}
                        User action: {{choiceText}}

                        Acknowledge the user's action directly and give it an observable consequence. Write 250-500 words of book-like prose.
                        Return only a JSON object with this schema:
                        {
                            "narrative": "string",
                            "sceneSummary": "string",
                            "location": "string",
                            "activeConflict": "string",
                            "storyState": {
                                "characters": [],
                                "relationships": [],
                                "facts": [],
                                "resources": [],
                                "unresolvedThreads": []
                            },
                            "suggestedActions": ["2 or 3 distinct optional actions"],
                            "storyBeat": "standard|opening|major|climax|conclusion",
                            "isEpisodeComplete": false
                        }
                        """;

        private static SceneDto ToDto(Scene scene)
                => new(
                        scene.Id,
                        scene.SessionId,
                        scene.SequenceNumber,
                        scene.ChoiceText,
                        scene.NarrativeText,
                        scene.SceneSummary,
                        scene.Location,
                        scene.ActiveConflict,
                        scene.StoryStateSchemaVersion,
                        DeserializeStoryState(scene.StoryStateJson),
                        DeserializeSuggestedActions(scene.SuggestedActionsJson),
                        scene.StoryBeat,
                        scene.IsEpisodeComplete,
                        scene.ImageUrl,
                        scene.ImageUrlExpiresAt,
                        scene.ModerationStatus,
                        scene.ModerationDetail,
                        scene.CreatedAt,
                        scene.UpdatedAt);

        private static IReadOnlyList<string> DeserializeSuggestedActions(string value)
            => string.IsNullOrWhiteSpace(value) ? [] : JsonSerializer.Deserialize<string[]>(value) ?? [];

        private static JsonElement DeserializeStoryState(string value)
            => JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(value) ? "{}" : value);
}
