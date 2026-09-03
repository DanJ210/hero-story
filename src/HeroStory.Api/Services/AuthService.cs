using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HeroStory.Api.DTOs.Auth;
using HeroStory.Core.Entities;
using HeroStory.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HeroStory.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IUserPortraitService _userPortraitService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext dbContext,
        IConfiguration configuration,
        IUserPortraitService userPortraitService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _userPortraitService = userPortraitService;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        return new RegisterResponse(user.Id, user.Email ?? request.Email, user.DisplayName);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return await IssueTokensAsync(user, null, cancellationToken);
    }

    public async Task<TokenResponse> DevelopmentLoginAsync(CancellationToken cancellationToken)
    {
        var email = _configuration["DEV_AUTH_EMAIL"] ?? "developer@hero-story.local";
        var displayName = _configuration["DEV_AUTH_DISPLAY_NAME"] ?? "Development Hero";
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName = displayName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }

        return await IssueTokensAsync(user, null, cancellationToken);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var existing = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.Token == tokenHash && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow, cancellationToken)
            ?? throw new UnauthorizedAccessException("Refresh token is invalid.");

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString())
            ?? throw new UnauthorizedAccessException("User not found.");

        return await IssueTokensAsync(user, existing, cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var existing = await _dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.Token == tokenHash, cancellationToken);
        if (existing is not null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountRequest request, string ipAddress, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.Include(x => x.Sessions).ThenInclude(x => x.Scenes).SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Password confirmation failed.");
        }

        var portraitPurge = await _userPortraitService.PurgeAsync(userId, cancellationToken);

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow.AddDays(30);
        _dbContext.DeletionAuditLogs.Add(new DeletionAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RequestedAt = DateTime.UtcNow,
            ExecutedAt = DateTime.UtcNow.AddDays(30),
            SessionsRemoved = user.Sessions.Count,
            ScenesRemoved = user.Sessions.Sum(x => x.Scenes.Count),
            BlobsRemoved = portraitPurge.BlobsRemoved,
            HashedIp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress)))
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TokenResponse> IssueTokensAsync(ApplicationUser user, RefreshToken? existingToken, CancellationToken cancellationToken)
    {
        var expiresMinutes = int.TryParse(_configuration["JWT_EXPIRES_MINUTES"], out var parsedMinutes) ? parsedMinutes : 15;
        var refreshDays = int.TryParse(_configuration["JWT_REFRESH_EXPIRES_DAYS"], out var parsedDays) ? parsedDays : 7;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);
        var accessToken = CreateAccessToken(user, expiresAtUtc);
        var refreshTokenRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = HashToken(refreshTokenRaw),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            ReplacedBy = null
        };

        if (existingToken is not null)
        {
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReplacedBy = refreshToken.Token;
        }

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TokenResponse(accessToken, refreshTokenRaw, expiresAtUtc);
    }

    private string CreateAccessToken(ApplicationUser user, DateTime expiresAtUtc)
    {
        var secret = _configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is required.");
        var issuer = _configuration["JWT_ISSUER"] ?? "hero-story";
        var audience = _configuration["JWT_AUDIENCE"] ?? "hero-story-client";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("display_name", user.DisplayName)
        };

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAtUtc, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashToken(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
