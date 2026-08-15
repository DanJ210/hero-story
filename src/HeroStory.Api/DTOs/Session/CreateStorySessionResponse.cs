using HeroStory.Api.DTOs.Scene;

namespace HeroStory.Api.DTOs.Session;

public sealed record CreateStorySessionResponse(SessionDto Session, SceneDto OpeningScene);