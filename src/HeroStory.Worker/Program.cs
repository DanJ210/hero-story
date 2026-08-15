using global::HeroStory.Api.Services;
using global::HeroStory.Infrastructure.Clients;
using global::HeroStory.Infrastructure.Data;
using global::HeroStory.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("Default")
    ?? configuration["DB_CONNECTION_STRING"]
    ?? throw new InvalidOperationException("ConnectionStrings:Default or DB_CONNECTION_STRING is required.");

builder.Services.Configure<global::HeroStory.Worker.WorkerOptions>(options =>
{
    options.PollIntervalSeconds = int.TryParse(configuration["AZURE_QUEUE_POLL_INTERVAL_SECONDS"], out var poll) ? poll : 5;
    options.MaxDequeueCount = int.TryParse(configuration["AZURE_QUEUE_MAX_DEQUEUE_COUNT"], out var max) ? max : 3;
});
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton<AzureQueueClient>();
builder.Services.AddSingleton<AzureBlobService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<global::HeroStory.Worker.IImageGeneratorStrategy, global::HeroStory.Worker.PlaceholderImageStrategy>();
builder.Services.AddScoped<global::HeroStory.Worker.IImageGeneratorStrategy, global::HeroStory.Worker.DallE3Strategy>();
builder.Services.AddHostedService<global::HeroStory.Worker.ImageGenerationWorker>();

var host = builder.Build();
host.Run();
