using System.Security.Claims;
using HeroStory.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeroStory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile/portrait")]
public class ProfileController : ControllerBase
{
    private readonly IUserPortraitService _portraitService;

    public ProfileController(IUserPortraitService portraitService)
    {
        _portraitService = portraitService;
    }

    [HttpGet]
    public async Task<ActionResult<PortraitDto>> Get(CancellationToken cancellationToken)
    {
        var portrait = await _portraitService.GetActiveAsync(GetUserId(), cancellationToken);
        return portrait is null ? NotFound() : Ok(portrait);
    }

    [HttpGet("content")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetContent(CancellationToken cancellationToken)
    {
        var portrait = await _portraitService.GetActiveContentAsync(GetUserId(), cancellationToken);
        return portrait is null ? NotFound() : File(portrait.Content, portrait.ContentType);
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<PortraitDto>> Upload(
        IFormFile file,
        [FromForm] bool consentGranted,
        [FromForm] string consentPolicyVersion,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await using var content = file.OpenReadStream();
        var portrait = await _portraitService.UploadAsync(
            userId, content, file.ContentType, file.Length, consentGranted, consentPolicyVersion, cancellationToken);
        return CreatedAtAction(nameof(Upload), portrait);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
        => await _portraitService.DeleteAsync(GetUserId(), cancellationToken) ? NoContent() : NotFound();

    [HttpPost("disable")]
    public async Task<IActionResult> Disable(CancellationToken cancellationToken)
        => await _portraitService.DisableAsync(GetUserId(), cancellationToken) ? NoContent() : NotFound();

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
}