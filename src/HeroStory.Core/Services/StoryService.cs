using HeroStory.Core.Abstractions;
using HeroStory.Core.Contracts;
using HeroStory.Core.Domain.Entities;
using HeroStory.Core.Domain.Enums;

namespace HeroStory.Core.Services;

public sealed class StoryService(
    IStoryRepository repository,
    IImageJobQueue imageJobQueue,
    IContentModerator moderator,
    IPlaceholderImageGenerator imageGenerator) : IStoryService
{
    public async Task<StoryDto> CreateAsync(CreateStoryRequest request, CancellationToken cancellationToken)
    {
        var combined = string.Join(" ", new[] { request.HeroName, request.Setting, request.Tone, request.Prompt });
        if (!moderator.IsAllowed(combined, out var reason))
        {
            var rejected = new HeroStoryAggregate
            {
                HeroName = request.HeroName.Trim(),
                Setting = request.Setting.Trim(),
                Tone = request.Tone.Trim(),
                Prompt = request.Prompt.Trim(),
                Status = StoryStatus.Rejected,
                FailureReason = reason
            };

            await repository.AddAsync(rejected, cancellationToken);
            return Map(rejected);
        }

        var story = BuildStory(request);
        await repository.AddAsync(story, cancellationToken);
        await imageJobQueue.QueueAsync(story.Id, cancellationToken);
        return Map(story);
    }

    public async Task<IReadOnlyCollection<StoryDto>> ListAsync(CancellationToken cancellationToken)
        => (await repository.ListAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<StoryDto?> GetAsync(Guid id, CancellationToken cancellationToken)
        => (await repository.GetAsync(id, cancellationToken)) is { } story ? Map(story) : null;

    public async Task<bool> ProcessImagesAsync(Guid storyId, CancellationToken cancellationToken)
    {
        var story = await repository.GetAsync(storyId, cancellationToken);
        if (story is null || story.Status is StoryStatus.Rejected)
        {
            return false;
        }

        story.Status = StoryStatus.GeneratingImage;
        story.Touch();
        await repository.UpdateAsync(story, cancellationToken);

        try
        {
            story.CoverImageUrl = await imageGenerator.GenerateCoverAsync(story, cancellationToken);
            var sceneImages = await imageGenerator.GenerateSceneImagesAsync(story, cancellationToken);
            for (var i = 0; i < story.Scenes.Count && i < sceneImages.Count; i++)
            {
                story.Scenes[i].ImageUrl = sceneImages[i];
            }

            story.Status = StoryStatus.Ready;
            story.FailureReason = null;
        }
        catch (Exception ex)
        {
            story.Status = StoryStatus.Failed;
            story.FailureReason = ex.Message;
        }

        story.Touch();
        await repository.UpdateAsync(story, cancellationToken);
        return true;
    }

    private static HeroStoryAggregate BuildStory(CreateStoryRequest request)
    {
        var heroName = request.HeroName.Trim();
        var setting = request.Setting.Trim();
        var tone = string.IsNullOrWhiteSpace(request.Tone) ? "hopeful" : request.Tone.Trim();
        var prompt = request.Prompt.Trim();

        var story = new HeroStoryAggregate
        {
            HeroName = heroName,
            Setting = setting,
            Tone = tone,
            Prompt = prompt,
            Status = StoryStatus.Draft
        };

        story.Scenes.AddRange([
            new StoryScene
            {
                Sequence = 1,
                Title = $"{heroName} gets the call",
                Narrative = $"In {setting}, {heroName} discovers a challenge that only a brave heart can solve. The tone is {tone} as the adventure begins.",
                ImagePrompt = $"Storybook illustration of {heroName} hearing the call to adventure in {setting}, {tone} mood"
            },
            new StoryScene
            {
                Sequence = 2,
                Title = "The challenge grows",
                Narrative = $"The path twists when {prompt}. {heroName} learns that courage and kindness are both part of the quest.",
                ImagePrompt = $"Storybook illustration of {heroName} facing a growing challenge in {setting}, inspired by: {prompt}"
            },
            new StoryScene
            {
                Sequence = 3,
                Title = "A hopeful ending",
                Narrative = $"By the end of the day, {heroName} turns the obstacle into a lesson, bringing hope back to {setting}.",
                ImagePrompt = $"Storybook illustration of {heroName} celebrating a hopeful ending in {setting}"
            }
        ]);

        return story;
    }

    private static StoryDto Map(HeroStoryAggregate story)
        => new()
        {
            Id = story.Id,
            HeroName = story.HeroName,
            Setting = story.Setting,
            Tone = story.Tone,
            Prompt = story.Prompt,
            Status = story.Status.ToString(),
            CoverImageUrl = story.CoverImageUrl,
            FailureReason = story.FailureReason,
            CreatedUtc = story.CreatedUtc,
            UpdatedUtc = story.UpdatedUtc,
            Scenes = story.Scenes
                .OrderBy(scene => scene.Sequence)
                .Select(scene => new StorySceneDto
                {
                    Sequence = scene.Sequence,
                    Title = scene.Title,
                    Narrative = scene.Narrative,
                    ImagePrompt = scene.ImagePrompt,
                    ImageUrl = scene.ImageUrl
                })
                .ToArray()
        };
}
