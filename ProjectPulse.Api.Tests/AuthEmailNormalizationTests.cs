using System.Net;
using System.Net.Http.Json;
using ProjectPulse.Api.DTOs;
using Xunit;

namespace ProjectPulse.Api.Tests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class AuthEmailNormalizationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEmailNormalizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegistrationAndLoginNormalizeEmailConsistently()
    {
        using var client = _factory.CreateClient();
        var id = Guid.NewGuid().ToString("N");
        var mixedCaseEmail = $"  Mixed-{id}@Example.Test  ";
        var normalizedEmail = mixedCaseEmail.Trim().ToLowerInvariant();
        const string password = "TestPass!123";

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register", new RegisterDto(mixedCaseEmail, password, "Mixed Case User"));
        registerResponse.EnsureSuccessStatusCode();
        var registeredUser = await registerResponse.Content.ReadFromJsonAsync<MeDto>();
        Assert.Equal(normalizedEmail, registeredUser?.Email);

        var loginResponse = await client.PostAsJsonAsync(
            "/auth/login", new LoginDto(normalizedEmail.ToUpperInvariant(), password));
        loginResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterDto($" {normalizedEmail.ToUpperInvariant()} ", password, "Duplicate User"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }
}
