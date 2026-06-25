using Xunit;

namespace ProjectPulse.Api.Tests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class StartupTests
{
    private readonly CustomWebApplicationFactory _factory;

    public StartupTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerJson_IsAvailable()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"status\":\"ok\"", await response.Content.ReadAsStringAsync());
    }
}
