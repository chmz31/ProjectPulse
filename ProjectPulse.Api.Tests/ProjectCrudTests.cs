using System.Net;
using System.Net.Http.Json;
using ProjectPulse.Api.DTOs;
using Xunit;

namespace ProjectPulse.Api.Tests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class ProjectCrudTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ProjectCrudTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthenticatedUserCanCreateListGetUpdateAndDeleteProject()
    {
        using var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(
            _factory, "crud");

        var createResponse = await client.PostAsJsonAsync(
            "/projects", new ProjectCreateDto("Original project", "Original description"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectDto>()
                      ?? throw new InvalidOperationException("Create response did not contain a project.");
        Assert.Equal("Original project", created.Name);
        Assert.Equal("Original description", created.Description);

        var projects = await client.GetFromJsonAsync<List<ProjectDto>>("/projects");
        Assert.Contains(projects!, p =>
            p.Id == created.Id &&
            p.Name == "Original project" &&
            p.Description == "Original description");

        var fetched = await client.GetFromJsonAsync<ProjectDto>($"/projects/{created.Id}");
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Original project", fetched.Name);
        Assert.Equal("Original description", fetched.Description);

        var updateResponse = await client.PutAsJsonAsync(
            $"/projects/{created.Id}", new ProjectUpdateDto("Updated project", "Updated description"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var updated = await client.GetFromJsonAsync<ProjectDto>($"/projects/{created.Id}");
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Updated project", updated.Name);
        Assert.Equal("Updated description", updated.Description);

        var deleteResponse = await client.DeleteAsync($"/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await client.GetAsync($"/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task ProjectEndpointsRequireAuthentication()
    {
        using var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var getResponse = await client.GetAsync("/projects");
        var postResponse = await client.PostAsJsonAsync(
            "/projects", new ProjectCreateDto("Project", null));
        var putResponse = await client.PutAsJsonAsync(
            $"/projects/{id}", new ProjectUpdateDto("Project", null));
        var deleteResponse = await client.DeleteAsync($"/projects/{id}");

        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ProjectCreateAndUpdateValidateRequiredName()
    {
        using var client = await IntegrationTestHelpers.CreateAuthenticatedClientAsync(
            _factory, "validation");
        var project = await IntegrationTestHelpers.CreateProjectAsync(client, "Valid project");

        var createResponse = await client.PostAsJsonAsync(
            "/projects", new ProjectCreateDto("", "Missing name"));
        var updateResponse = await client.PutAsJsonAsync(
            $"/projects/{project.Id}", new ProjectUpdateDto("", "Missing name"));

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }
}
