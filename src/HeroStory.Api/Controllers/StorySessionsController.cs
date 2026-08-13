using System.Security.Claims;
using HeroStory.Api.DTOs.Session;
using HeroStory.Api.Services;
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

    public StorySessionsController(IStoryService storyService)
    {
        _storyService = storyService;
    }

    [HttpGet]
    public Task<IReadOnlyList<SessionListDto>> GetSessions(CancellationToken cancellationToken)
        => _storyService.GetSessionsAsync(GetUserId(), cancellationToken);

    [HttpPost]
    [EnableRateLimiting("sessions")]
    public async Task<ActionResult<SessionDto>> CreateSession(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var response = await _storyService.CreateSessionAsync(GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionDto>> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var response = await _storyService.GetSessionAsync(GetUserId(), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<SessionDto>> PatchSession(Guid id, PatchSessionRequest request, CancellationToken cancellationToken)
    {
        var response = await _storyService.PatchSessionAsync(GetUserId(), id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
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
