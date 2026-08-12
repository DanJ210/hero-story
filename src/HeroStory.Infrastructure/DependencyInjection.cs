using HeroStory.Core.Abstractions;
using HeroStory.Infrastructure.Options;
using HeroStory.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HeroStory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHeroStoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ModerationOptions>(options =>
        {
            options.KeywordListPath = configuration["Moderation:KeywordListPath"]
                ?? configuration["MODERATION_KEYWORD_LIST_PATH"]
                ?? "config/keywords.txt";
        });

        services.AddSingleton<IStoryRepository, InMemoryStoryRepository>();
        services.AddSingleton<IImageJobQueue, InMemoryImageJobQueue>();
        services.AddSingleton<IContentModerator, FileKeywordModerator>();
        services.AddSingleton<IPlaceholderImageGenerator, PlaceholderImageGenerator>();
        return services;
    }
}
