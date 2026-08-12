using HeroStory.Api.Contracts;
using HeroStory.Core.Abstractions;
using HeroStory.Core.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace HeroStory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StoriesController(IStoryService storyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StoryDto>>> ListAsync(CancellationToken cancellationToken)
        => Ok(await storyService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StoryDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var story = await storyService.GetAsync(id, cancellationToken);
        return story is null ? NotFound() : Ok(story);
    }

    [HttpPost]
    public async Task<ActionResult<StoryDto>> CreateAsync(CreateStoryApiRequest request, CancellationToken cancellationToken)
    {
        var story = await storyService.CreateAsync(new CreateStoryRequest
        {
            HeroName = request.HeroName,
            Setting = request.Setting,
            Tone = request.Tone,
            Prompt = request.Prompt
        }, cancellationToken);

        return Created($"/api/stories/{story.Id}", story);
    }
}
