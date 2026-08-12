using HeroStory.Core.Enums; namespace HeroStory.Api.DTOs.Session; public sealed record SessionListDto(Guid Id, string Title, string Genre, string HeroName, SessionStatus Status, DateTime UpdatedAt);
