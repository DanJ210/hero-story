using HeroStory.Api.Services;
using HeroStory.Core.Enums;

namespace HeroStory.UnitTests.Services;

public class StoryTurnResponseParserTests
{
    [Fact]
    public void Parse_ReturnsValidatedStructuredTurn()
    {
        const string response = """
            {
              "narrative": "You step into the reactor chamber as the bridge shakes.",
              "sceneSummary": "The hero reaches the unstable reactor.",
              "location": "Skybridge reactor chamber",
              "activeConflict": "Stop the reactor before the bridge collapses",
              "storyState": {
                "facts": ["The engineer knows the hero's identity"],
                "unresolvedThreads": ["Who sabotaged the reactor?"]
              },
              "suggestedActions": ["Disable the reactor", "Confront the engineer", "Rescue the workers"],
              "storyBeat": "major",
              "isEpisodeComplete": false
            }
            """;

        var result = StoryTurnResponseParser.Parse(response);

        Assert.Equal("The hero reaches the unstable reactor.", result.SceneSummary);
        Assert.Equal(3, result.SuggestedActions.Count);
        Assert.Equal(StoryBeat.Major, result.StoryBeat);
        Assert.Contains("engineer", result.StoryStateJson);
    }

    [Fact]
    public void Parse_RejectsResponseWithoutTwoSuggestedActions()
    {
        const string response = """
            {
              "narrative": "You enter the chamber.",
              "sceneSummary": "The hero enters the chamber.",
              "location": "Reactor chamber",
              "activeConflict": "Stop the reactor",
              "storyState": {},
              "suggestedActions": ["Disable the reactor"],
              "storyBeat": "standard",
              "isEpisodeComplete": false
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => StoryTurnResponseParser.Parse(response));

        Assert.Contains("2 or 3", exception.Message);
    }
}