using System.Linq;
using HeroStory.Api.DTOs.Auth;
using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HeroStory.UnitTests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ReturnsCreatedUser()
    {
        var dbContext = CreateDbContext();
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var portraitService = new Mock<IUserPortraitService>();
        var configuration = CreateConfiguration();
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        var service = new AuthService(userManager.Object, signInManager.Object, dbContext, configuration, portraitService.Object);
        var response = await service.RegisterAsync(new RegisterRequest("hero@example.com", "Password1", "Hero"), CancellationToken.None);

        Assert.Equal("hero@example.com", response.Email);
        Assert.Equal("Hero", response.DisplayName);
    }

    [Fact]
    public async Task DeleteAccountAsync_PurgesPortraitsAndWritesBlobAuditCount()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "hero@example.com",
            Email = "hero@example.com",
            DisplayName = "Hero",
            CreatedAt = DateTime.UtcNow,
            Sessions =
            [
                new StorySession
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Title = "Story",
                    Genre = "Fantasy",
                    HeroArchetype = "Guardian",
                    HeroName = "Ari",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Scenes =
                    [
                        new Scene
                        {
                            Id = Guid.NewGuid(),
                            SessionId = Guid.NewGuid(),
                            SequenceNumber = 1,
                            ChoiceText = "Start",
                            NarrativeText = "Narrative",
                            SceneSummary = "Summary",
                            Location = "City",
                            ActiveConflict = "Conflict",
                            StoryStateJson = "{}",
                            SuggestedActionsJson = "[]",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    ]
                }
            ]
        };
        user.Sessions.First().UserId = user.Id;
        user.Sessions.First().Scenes.First().SessionId = user.Sessions.First().Id;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.SetupGet(manager => manager.Users).Returns(dbContext.Users);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, "Password1")).ReturnsAsync(true);
        var signInManager = CreateSignInManager(userManager.Object);
        var portraitService = new Mock<IUserPortraitService>();
        portraitService
            .Setup(service => service.PurgeAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortraitPurgeResult(2, 2, 3));
        var serviceUnderTest = new AuthService(userManager.Object, signInManager.Object, dbContext, CreateConfiguration(), portraitService.Object);

        await serviceUnderTest.DeleteAccountAsync(user.Id, new DeleteAccountRequest("Password1"), "127.0.0.1", CancellationToken.None);

        var deletionAudit = await dbContext.DeletionAuditLogs.SingleAsync();
        Assert.True(user.IsDeleted);
        Assert.Equal(2, deletionAudit.BlobsRemoved);
        Assert.Equal(1, deletionAudit.SessionsRemoved);
        Assert.Equal(1, deletionAudit.ScenesRemoved);
        portraitService.Verify(service => service.PurgeAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["JWT_SECRET"] = "12345678901234567890123456789012",
        ["JWT_ISSUER"] = "issuer",
        ["JWT_AUDIENCE"] = "audience"
    }).Build();

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(UserManager<ApplicationUser> userManager)
    {
        return new Mock<SignInManager<ApplicationUser>>(userManager, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), null!, null!, null!, null!);
    }
}
