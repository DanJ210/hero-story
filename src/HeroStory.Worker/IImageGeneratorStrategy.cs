using HeroStory.Core.Entities;

namespace HeroStory.Worker;

public interface IImageGeneratorStrategy
{
    string Name { get; }
    Task GenerateAsync(GenerationJob job, CancellationToken cancellationToken);
}
