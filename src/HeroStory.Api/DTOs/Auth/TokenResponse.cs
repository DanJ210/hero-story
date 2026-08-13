namespace HeroStory.Api.DTOs.Auth; public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
