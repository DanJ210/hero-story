using System.Security.Claims;
using HeroStory.Api.DTOs.Auth;
using HeroStory.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HeroStory.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public Task<TokenResponse> Login(LoginRequest request, CancellationToken cancellationToken)
        => _authService.LoginAsync(request, cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<TokenResponse> Refresh(RefreshRequest request, CancellationToken cancellationToken)
        => _authService.RefreshAsync(request, cancellationToken);

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
        await _authService.DeleteAccountAsync(userId, request, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", cancellationToken);
        return Accepted();
    }
}
