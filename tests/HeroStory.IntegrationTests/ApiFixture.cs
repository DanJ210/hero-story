using HeroStory.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HeroStory.IntegrationTests;

public class ApiFixture : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected virtual string EnvironmentName => "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("JWT_SECRET", "integration-test-signing-key-32-bytes");
        builder.UseSetting("DB_APPLY_MIGRATIONS", "false");
        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(service => service.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || service.ServiceType == typeof(DbContextOptions)
                    || service.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>))
                .ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}

public sealed class DevelopmentApiFixture : ApiFixture
{
    protected override string EnvironmentName => "Development";
}
