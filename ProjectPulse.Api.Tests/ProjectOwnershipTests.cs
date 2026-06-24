using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProjectPulse.Api.DTOs;
using Xunit;

namespace ProjectPulse.Api.Tests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class ProjectOwnershipTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ProjectOwnershipTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UserCannotReadUpdateOrDeleteAnotherUsersProject()
    {
        using var userA = await CreateAuthenticatedClientAsync();
        using var userB = await CreateAuthenticatedClientAsync();
        var project = await CreateProjectAsync(userB, "User B project");

        var getResponse = await userA.GetAsync($"/projects/{project.Id}");
        var updateResponse = await userA.PutAsJsonAsync(
            $"/projects/{project.Id}", new ProjectUpdateDto("Changed", null));
        var deleteResponse = await userA.DeleteAsync($"/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        var ownerResponse = await userB.GetAsync($"/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task UsersOnlySeeTheirOwnProjects()
    {
        using var userA = await CreateAuthenticatedClientAsync();
        using var userB = await CreateAuthenticatedClientAsync();
        var projectA = await CreateProjectAsync(userA, "User A project");
        var projectB = await CreateProjectAsync(userB, "User B project");

        var projectsA = await userA.GetFromJsonAsync<List<ProjectDto>>("/projects");
        var projectsB = await userB.GetFromJsonAsync<List<ProjectDto>>("/projects");

        Assert.Equal(projectA.Id, Assert.Single(projectsA!).Id);
        Assert.Equal(projectB.Id, Assert.Single(projectsB!).Id);
    }

    [Fact]
    public async Task UnauthenticatedProjectRequestReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid().ToString("N");
        var email = $"user-{id}@example.test";
        const string password = "TestPass!123";

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register", new RegisterDto(email, password, $"User {id}"));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginDto(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>()
                     ?? throw new InvalidOperationException("Login response did not contain tokens.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    private static async Task<ProjectDto> CreateProjectAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/projects", new ProjectCreateDto(name, null));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProjectDto>()
               ?? throw new InvalidOperationException("Create response did not contain a project.");
    }
}
