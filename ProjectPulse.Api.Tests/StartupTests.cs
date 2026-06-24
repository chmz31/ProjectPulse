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
}
