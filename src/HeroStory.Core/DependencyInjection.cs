using HeroStory.Core.Abstractions;
using HeroStory.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HeroStory.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddHeroStoryCore(this IServiceCollection services)
    {
        services.AddScoped<IStoryService, StoryService>();
        return services;
    }
}
