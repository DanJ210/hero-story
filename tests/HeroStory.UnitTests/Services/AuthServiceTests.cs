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
        var configuration = CreateConfiguration();
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        var service = new AuthService(userManager.Object, signInManager.Object, dbContext, configuration);
        var response = await service.RegisterAsync(new RegisterRequest("hero@example.com", "Password1", "Hero"), CancellationToken.None);

        Assert.Equal("hero@example.com", response.Email);
        Assert.Equal("Hero", response.DisplayName);
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
