using System.Net;
using System.Net.Http.Json;
using ProjectPulse.Api.DTOs;
using Xunit;

namespace ProjectPulse.Api.Tests;

public sealed class AuthRateLimitTests
{
    [Fact]
    public async Task RepeatedLoginAttemptsReturnTooManyRequests()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new LoginDto("rate-limit@example.test", "TestPass!123");

        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/auth/login", request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using var limitedResponse = await client.PostAsJsonAsync("/auth/login", request);
        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
    }
}
