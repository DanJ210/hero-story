using HeroStory.Api.DTOs.Scene;

namespace HeroStory.Api.DTOs.Session;

public sealed record StoryWorkspaceDto(SessionDto Session, IReadOnlyList<SceneDto> Turns);