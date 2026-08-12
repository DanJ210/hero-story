using HeroStory.Core.Entities;

namespace HeroStory.Worker;

public class DallE3Strategy : IImageGeneratorStrategy
{
    public string Name => "dalle3";

    public Task GenerateAsync(GenerationJob job, CancellationToken cancellationToken)
    {
        // Week 4
        throw new NotImplementedException("Week 4");
    }
}
