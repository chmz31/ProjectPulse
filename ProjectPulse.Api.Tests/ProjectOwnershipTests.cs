using System.Net;
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
        using var userA = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory, "owner-a");
        using var userB = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory, "owner-b");
        var project = await IntegrationTestHelpers.CreateProjectAsync(userB, "User B project");

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
        using var userA = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory, "list-a");
        using var userB = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(_factory, "list-b");
        var projectA = await IntegrationTestHelpers.CreateProjectAsync(userA, "User A project");
        var projectB = await IntegrationTestHelpers.CreateProjectAsync(userB, "User B project");

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
}
