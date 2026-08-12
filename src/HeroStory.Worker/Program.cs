using HeroStory.Core;
using HeroStory.Infrastructure;
using HeroStory.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHeroStoryCore();
builder.Services.AddHeroStoryInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ImageGenerationWorker>();

var host = builder.Build();
host.Run();
