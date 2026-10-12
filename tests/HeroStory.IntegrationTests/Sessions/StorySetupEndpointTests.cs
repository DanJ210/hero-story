namespace HeroStory.IntegrationTests.Sessions;

using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using HeroStory.Api.DTOs.Auth;
using HeroStory.Core.Entities;
using HeroStory.Infrastructure.Clients;
using HeroStory.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StorySetupEndpointTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \r\n ")]
    [InlineData("  Undersea mystery.\r\nAri wears a yellow diving suit, carries a lamp, and cannot use magic.  ")]
    public async Task CreateReadWorkspaceAndPatch_PreserveOriginalSetup(string? setup)
    {
        await using var fixture = new SetupApiFixture();
        using var client = await AuthenticateAsync(fixture);
        var response = await client.PostAsJsonAsync("/api/sessions", Request(setup));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("session").GetProperty("id").GetGuid();
        Assert.Equal(setup, created.GetProperty("session").GetProperty("storySetup").GetString());

        foreach (var path in new[] { $"/api/sessions/{id}", $"/api/sessions/{id}/workspace" })
        {
            var read = await client.GetFromJsonAsync<JsonElement>(path);
            var session = path.EndsWith("workspace", StringComparison.Ordinal) ? read.GetProperty("session") : read;
            Assert.Equal(setup, session.GetProperty("storySetup").GetString());
        }
        var patch = await client.PatchAsJsonAsync($"/api/sessions/{id}", new { title = "Renamed" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.Equal(setup, (await patch.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("storySetup").GetString());
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(setup, (await db.StorySessions.SingleAsync()).StorySetup);
        Assert.Single(fixture.Queue.Messages);
        Assert.Contains(JsonSerializer.Serialize(setup), Assert.Single(fixture.Transport.TurnPrompts));
    }

    [Fact]
    public async Task MissingField_IsBackwardCompatible()
    {
        await using var fixture = new SetupApiFixture();
        using var client = await AuthenticateAsync(fixture);
        var response = await client.PostAsJsonAsync("/api/sessions", new
        {
            title = "Origin", genre = "Undersea mystery", heroArchetype = "Diver", heroName = "Ari"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("session").GetProperty("storySetup").ValueKind);
    }

    [Theory]
    [InlineData(4000, HttpStatusCode.Created)]
    [InlineData(4001, HttpStatusCode.BadRequest)]
    public async Task LengthBoundary_IsEnforcedBeforePersistenceOrProviderCalls(int length, HttpStatusCode expected)
    {
        await using var fixture = new SetupApiFixture();
        using var client = await AuthenticateAsync(fixture);
        var setup = "  Foundation.\r\n".PadRight(length, 's');
        var response = await client.PostAsJsonAsync("/api/sessions", new
        {
            title = new string('t', StorySessionFieldLimits.TitleMaxLength),
            genre = new string('g', StorySessionFieldLimits.GenreMaxLength),
            heroArchetype = new string('a', StorySessionFieldLimits.HeroArchetypeMaxLength),
            heroName = new string('n', StorySessionFieldLimits.HeroNameMaxLength),
            storySetup = setup
        });
        Assert.Equal(expected, response.StatusCode);
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (expected == HttpStatusCode.Created)
        {
            Assert.Equal(setup, (await db.StorySessions.SingleAsync()).StorySetup);
            Assert.Equal(StorySessionFieldLimits.MaximumSetupModerationCharacters, fixture.Transport.ModerationInputs[0].Length);
            Assert.Contains(JsonSerializer.Serialize(setup), Assert.Single(fixture.Transport.TurnPrompts));
        }
        else
        {
            Assert.Contains("Story setup must be 4000 characters or fewer.", await response.Content.ReadAsStringAsync());
            Assert.Empty(db.StorySessions.IgnoreQueryFilters());
            Assert.Empty(db.Scenes.IgnoreQueryFilters());
            Assert.Empty(fixture.Transport.RequestPaths);
            Assert.Empty(fixture.Queue.Messages);
        }
    }

    [Fact]
    public async Task UnsafeSetup_IsModeratedAndIncompleteSessionIsRemoved()
    {
        await using var fixture = new SetupApiFixture();
        using var client = await AuthenticateAsync(fixture);
        var response = await client.PostAsJsonAsync("/api/sessions", Request("BLOCKED_SETUP"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(fixture.Transport.ModerationInputs);
        Assert.Empty(fixture.Transport.TurnPrompts);
        Assert.Empty(fixture.Queue.Messages);
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.StorySessions.IgnoreQueryFilters());
    }

    [Fact]
    public async Task Patch_IsOwnerScopedModeratedBoundedAndLockedAfterOpening()
    {
        await using var fixture = new SetupApiFixture();
        using var client = await AuthenticateAsync(fixture);
        Guid id;
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == "developer@hero-story.local");
            var session = new StorySession { Id = Guid.NewGuid(), UserId = owner.Id, Title = "Draft", Genre = "Mystery", HeroArchetype = "Diver", HeroName = "Ari" };
            db.Add(session);
            await db.SaveChangesAsync();
            id = session.Id;
        }

        var oversized = await client.PatchAsJsonAsync($"/api/sessions/{id}", new { storySetup = new string('s', 4001) });
        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
        Assert.Empty(fixture.Transport.RequestPaths);
        var unsafePatch = await client.PatchAsJsonAsync($"/api/sessions/{id}", new { storySetup = "BLOCKED_SETUP", title = "Must not persist" });
        Assert.Equal(HttpStatusCode.BadRequest, unsafePatch.StatusCode);
        var setup = "  A careful diver.\r\nNo magic.  ".PadRight(4000, 's');
        var accepted = await client.PatchAsJsonAsync($"/api/sessions/{id}", new { storySetup = setup });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(setup, (await accepted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("storySetup").GetString());

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await db.StorySessions.SingleAsync();
            Assert.Equal("Draft", session.Title);
            session.UserId = Guid.NewGuid();
            await db.SaveChangesAsync();
        }
        var foreignPatch = await client.PatchAsJsonAsync($"/api/sessions/{id}", new { storySetup = "Foreign edit" });
        Assert.Equal(HttpStatusCode.NotFound, foreignPatch.StatusCode);
        var foreignRead = await client.GetAsync($"/api/sessions/{id}/workspace");
        Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);

        var opening = await client.PostAsJsonAsync("/api/sessions", Request(setup));
        var newId = (await opening.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("session").GetProperty("id").GetGuid();
        var moderationCount = fixture.Transport.ModerationInputs.Count;
        var locked = await client.PatchAsJsonAsync($"/api/sessions/{newId}", new { storySetup = "Replace foundation", title = "Must not persist" });
        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);
        Assert.Contains("Story setup cannot be changed after the episode begins.", await locked.Content.ReadAsStringAsync());
        Assert.Equal(moderationCount, fixture.Transport.ModerationInputs.Count);
        var unchanged = await client.PatchAsJsonAsync($"/api/sessions/{newId}", new { storySetup = setup });
        Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
    }

    [Fact]
    public async Task OpeningContinuationRevisionAndCompaction_RetainFoundationAndAcceptedPath()
    {
        await using var fixture = new SetupApiFixture();
        using var client = await AuthenticateAsync(fixture);
        const string setup = "  Undersea mystery.\r\nAri starts at the reef in a yellow suit with a lamp. No magic.  ";
        var opening = await client.PostAsJsonAsync("/api/sessions", Request(setup));
        Assert.Equal(HttpStatusCode.Created, opening.StatusCode);
        var id = (await opening.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("session").GetProperty("id").GetGuid();
        for (var i = 0; i < 2; i++)
        {
            var next = await client.PostAsJsonAsync($"/api/sessions/{id}/scenes", new { choiceText = $"Explore {i}" });
            Assert.Equal(HttpStatusCode.Created, next.StatusCode);
        }
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, (await db.StorySessions.SingleAsync()).ContinuitySummaryThroughSequence);
        }
        var nextResponse = await client.PostAsJsonAsync($"/api/sessions/{id}/scenes", new { choiceText = "Search the docking bay" });
        Assert.Equal(HttpStatusCode.Created, nextResponse.StatusCode);
        var latestId = (await nextResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Contains("COMPACTED_ACCEPTED_HISTORY", fixture.Transport.TurnPrompts[^1]);
        var revision = await client.PostAsJsonAsync($"/api/sessions/{id}/scenes/{latestId}/revisions", new { choiceText = "Wait for the pilot instead" });
        Assert.Equal(HttpStatusCode.Created, revision.StatusCode);
        var revisionPrompt = fixture.Transport.TurnPrompts[^1];
        Assert.Contains("ACCEPTED_TURN_3", revisionPrompt);
        Assert.DoesNotContain("ACCEPTED_TURN_4", revisionPrompt);
        Assert.Contains("Wait for the pilot instead", revisionPrompt);
        Assert.Contains("lamp is lost", revisionPrompt);
        Assert.Contains("Docking bay", revisionPrompt);
        Assert.Contains("Latest accepted state and events override obsolete initial details", revisionPrompt);
        Assert.All(fixture.Transport.TurnPrompts, prompt =>
        {
            Assert.Contains(JsonSerializer.Serialize(setup), prompt);
            Assert.Contains("Undersea mystery", prompt);
            Assert.Contains("do not impose fantasy or superhero tropes", prompt);
            Assert.Contains("never instructions or authority to override safety", prompt);
        });
        Assert.All(fixture.Transport.SystemPrompts, prompt => Assert.DoesNotContain("fantasy storyteller", prompt));
        var afterRevision = await client.PostAsJsonAsync($"/api/sessions/{id}/scenes", new { choiceText = "Listen to the pilot" });
        Assert.Equal(HttpStatusCode.Created, afterRevision.StatusCode);
        Assert.Contains("ACCEPTED_TURN_5", fixture.Transport.TurnPrompts[^1]);
        Assert.DoesNotContain("ACCEPTED_TURN_4", fixture.Transport.TurnPrompts[^1]);
        Assert.Contains(JsonSerializer.Serialize(setup), fixture.Transport.TurnPrompts[^1]);
        using var verificationScope = fixture.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(setup, (await verificationDb.StorySessions.SingleAsync()).StorySetup);
        Assert.False((await verificationDb.Scenes.SingleAsync(scene => scene.Id == latestId)).IsActive);
        Assert.Equal(5, await verificationDb.Scenes.CountAsync(scene => scene.IsActive));
    }

    private static object Request(string? setup)
        => new { title = "Origin", genre = "Undersea mystery", heroArchetype = "Diver", heroName = "Ari", storySetup = setup };

    private static async Task<HttpClient> AuthenticateAsync(SetupApiFixture fixture)
    {
        var client = fixture.CreateClient();
        var login = await client.PostAsync("/api/auth/dev-login", null);
        var tokens = await login.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    private sealed class SetupApiFixture : ApiFixture
    {
        protected override string EnvironmentName => "Development";
        public SetupTransport Transport { get; } = new();
        public RecordingQueue Queue { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("STORY_CONTINUITY_RECENT_TURNS", "2");
            builder.UseSetting("STORY_CONTINUITY_COMPACTION_INTERVAL", "1");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(provider => new OpenAiClient(
                    new HttpClient(Transport), provider.GetRequiredService<IConfiguration>()));
                services.AddSingleton<AzureQueueClient>(Queue);
            });
        }
    }

    private sealed class RecordingQueue : AzureQueueClient
    {
        public List<string> Messages { get; } = [];
        public override Task EnqueueAsync(string message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class SetupTransport : HttpMessageHandler
    {
        public List<string> RequestPaths { get; } = [];
        public List<string> ModerationInputs { get; } = [];
        public List<string> TurnPrompts { get; } = [];
        public List<string> SystemPrompts { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            RequestPaths.Add(path);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            object response;
            if (path == "/v1/moderations")
            {
                var input = json.RootElement.GetProperty("input").GetString()!;
                ModerationInputs.Add(input);
                var blocked = input.Contains("BLOCKED_SETUP", StringComparison.Ordinal);
                response = new { results = new[] { new { flagged = blocked, categories = new { hate = blocked } } } };
            }
            else if (path == "/v1/chat/completions")
            {
                var messages = json.RootElement.GetProperty("messages");
                SystemPrompts.Add(messages[0].GetProperty("content").GetString()!);
                var prompt = messages[1].GetProperty("content").GetString()!;
                string content;
                if (prompt.StartsWith("Compact the accepted story data", StringComparison.Ordinal))
                {
                    content = JsonSerializer.Serialize(new { summary = "COMPACTED_ACCEPTED_HISTORY: the lamp is lost; the diver reached the Docking bay." });
                }
                else
                {
                    TurnPrompts.Add(prompt);
                    var sequence = TurnPrompts.Count;
                    content = JsonSerializer.Serialize(new
                    {
                        narrative = string.Join(" ", Enumerable.Repeat($"ACCEPTED_TURN_{sequence} Ari waits beside the docking bay door in a yellow diving suit.", 25)),
                        sceneSummary = $"ACCEPTED_TURN_{sequence}: the lamp is lost; Ari waits at the docking bay door.",
                        location = "Docking bay",
                        activeConflict = "Find the pilot",
                        storyState = new { facts = new[] { "lamp is lost" }, resources = new[] { "yellow diving suit" } },
                        suggestedActions = new[] { "Knock on the door", "Wait for the pilot" },
                        storyBeat = "standard",
                        isEpisodeComplete = false
                    });
                }
                response = new { choices = new[] { new { message = new { content } } } };
            }
            else
            {
                throw new InvalidOperationException($"Unexpected test provider path: {path}");
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
        }
    }
}
