using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProjectPulse.Api.DTOs;

namespace ProjectPulse.Api.Tests;

internal static class IntegrationTestHelpers
{
    public const string TestPassword = "TestPass!123";

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory,
        string emailPrefix = "user")
    {
        var client = factory.CreateClient();
        var id = Guid.NewGuid().ToString("N");
        var email = $"{emailPrefix}-{id}@example.test";

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register", new RegisterDto(email, TestPassword, $"User {id}"));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync(
            "/auth/login", new LoginDto(email, TestPassword));
        loginResponse.EnsureSuccessStatusCode();
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>()
                     ?? throw new InvalidOperationException("Login response did not contain tokens.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    public static async Task<ProjectDto> CreateProjectAsync(
        HttpClient client,
        string name,
        string? description = null)
    {
        var response = await client.PostAsJsonAsync(
            "/projects", new ProjectCreateDto(name, description));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProjectDto>()
               ?? throw new InvalidOperationException("Create response did not contain a project.");
    }
}
