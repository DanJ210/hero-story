using System.Text;
using HeroStory.Api.Middleware;
using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Infrastructure.Clients;
using HeroStory.Infrastructure.Data;
using HeroStory.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen();
}
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? builder.Configuration["SQLSERVER_CONNECTION_STRING"];

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseInMemoryDatabase("hero-story");
    }
});
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (builder.Environment.IsDevelopment())
    {
        jwtSecret = "12345678901234567890123456789012";
    }
    else
    {
        throw new InvalidOperationException("JWT_SECRET is required.");
    }
}
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? "http://localhost:8080",
            ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? "http://localhost:5173",
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddCors(options =>
    options.AddPolicy("default", policy =>
    {
        var origins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:5173")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    }));

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("register", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromHours(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromHours(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("sessions", limiter =>
    {
        limiter.PermitLimit = 50;
        limiter.Window = TimeSpan.FromDays(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("scenes", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromDays(1);
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddHttpClient<OpenAiClient>();
builder.Services.AddSingleton<AzureQueueClient>();
builder.Services.AddSingleton<AzureBlobService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddScoped<ISceneService, SceneService>();
builder.Services.AddScoped<IModerationService, ModerationService>();
builder.Services.AddScoped<IOpenAiTextService, OpenAiTextService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => c.SerializeAsV2 = true);
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob:; connect-src 'self' http://localhost:8080; style-src 'self' 'unsafe-inline'; script-src 'self';";
    await next();
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
