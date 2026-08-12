using System.Net.Http;

namespace HeroStory.IntegrationTests.Helpers;

internal static class ResponseDebugHelper
{
    public static async Task<string> ReadBodyAsync(HttpResponseMessage response)
        => await response.Content.ReadAsStringAsync();
}
