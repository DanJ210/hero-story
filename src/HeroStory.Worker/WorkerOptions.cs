namespace HeroStory.Worker;

public class WorkerOptions
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int MaxDequeueCount { get; set; } = 3;
}
