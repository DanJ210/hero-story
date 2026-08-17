using System.Security.Claims;
using HeroStory.Api.DTOs.Session;
using HeroStory.Api.DTOs.Scene;
using HeroStory.Api.Services;
using HeroStory.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HeroStory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sessions")]
public class StorySessionsController : ControllerBase
{
    private readonly IStoryService _storyService;
    private readonly IStoryCreationService _storyCreationService;
    private readonly ISceneService _sceneService;

    public StorySessionsController(IStoryService storyService, IStoryCreationService storyCreationService, ISceneService sceneService)
    {
        _storyService = storyService;
        _storyCreationService = storyCreationService;
        _sceneService = sceneService;
    }

    [HttpGet]
    public Task<IReadOnlyList<SessionListDto>> GetSessions(CancellationToken cancellationToken)
        => _storyService.GetSessionsAsync(GetUserId(), cancellationToken);

    [HttpPost]
    [EnableRateLimiting("sessions")]
    public async Task<ActionResult<CreateStorySessionResponse>> CreateSession(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var response = await _storyCreationService.CreateAsync(GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { id = response.Session.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionDto>> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var response = await _storyService.GetSessionAsync(GetUserId(), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/workspace")]
    public async Task<ActionResult<StoryWorkspaceDto>> GetWorkspace(Guid id, CancellationToken cancellationToken)
    {
        var response = await _storyService.GetWorkspaceAsync(GetUserId(), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<SessionDto>> PatchSession(Guid id, PatchSessionRequest request, CancellationToken cancellationToken)
    {
        var response = await _storyService.PatchSessionAsync(GetUserId(), id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/pause")]
    public async Task<ActionResult<SessionDto>> PauseSession(Guid id, CancellationToken cancellationToken)
    {
        var response = await _storyService.TransitionStatusAsync(GetUserId(), id, SessionStatus.Paused, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<ActionResult<SessionDto>> ResumeSession(Guid id, CancellationToken cancellationToken)
    {
        var response = await _storyService.TransitionStatusAsync(GetUserId(), id, SessionStatus.Active, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/conclusion")]
    [EnableRateLimiting("scenes")]
    public async Task<ActionResult<SceneDto>> ConcludeEpisode(Guid id, CancellationToken cancellationToken)
    {
        var response = await _sceneService.ConcludeEpisodeAsync(GetUserId(), id, cancellationToken);
        return CreatedAtAction(nameof(GetWorkspace), new { id }, response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSession(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _storyService.DeleteSessionAsync(GetUserId(), id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
}
