using System.Linq;
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
              "narrative": "NARRATIVE_PLACEHOLDER",
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

        var result = StoryTurnResponseParser.Parse(response.Replace("NARRATIVE_PLACEHOLDER", CreateNarrative()));

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
              "narrative": "NARRATIVE_PLACEHOLDER",
              "sceneSummary": "The hero enters the chamber.",
              "location": "Reactor chamber",
              "activeConflict": "Stop the reactor",
              "storyState": {},
              "suggestedActions": ["Disable the reactor"],
              "storyBeat": "standard",
              "isEpisodeComplete": false
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => StoryTurnResponseParser.Parse(response.Replace("NARRATIVE_PLACEHOLDER", CreateNarrative())));

        Assert.Contains("2 or 3", exception.Message);
    }

      [Theory]
      [InlineData(249)]
      [InlineData(501)]
      public void Parse_RejectsNarrativeOutsideWordLimit(int wordCount)
      {
        var response = CreateResponse(CreateNarrative(wordCount), "{}");

        var exception = Assert.Throws<InvalidOperationException>(() => StoryTurnResponseParser.Parse(response));

        Assert.Contains("250-500 words", exception.Message);
      }

      [Fact]
      public void Parse_RejectsOversizedStoryState()
      {
        var state = System.Text.Json.JsonSerializer.Serialize(new { facts = new[] { new string('x', 16_500) } });
        var response = CreateResponse(CreateNarrative(), state);

        var exception = Assert.Throws<InvalidOperationException>(() => StoryTurnResponseParser.Parse(response));

        Assert.Contains("storyState", exception.Message);
        Assert.Contains("bytes", exception.Message);
      }

      private static string CreateResponse(string narrative, string state)
        => $$"""
          {
            "narrative": "{{narrative}}",
            "sceneSummary": "The hero enters the chamber.",
            "location": "Reactor chamber",
            "activeConflict": "Stop the reactor",
            "storyState": {{state}},
            "suggestedActions": ["Disable the reactor", "Rescue the workers"],
            "storyBeat": "standard",
            "isEpisodeComplete": false
          }
          """;

      private static string CreateNarrative(int wordCount = 250)
        => string.Join(' ', Enumerable.Repeat("hero", wordCount));
}