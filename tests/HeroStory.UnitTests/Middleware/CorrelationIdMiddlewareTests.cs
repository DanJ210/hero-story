using HeroStory.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace HeroStory.UnitTests.Middleware;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsCorrelationHeader()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);
        Assert.True(context.Response.Headers.ContainsKey(CorrelationIdMiddleware.HeaderName));
    }
}
