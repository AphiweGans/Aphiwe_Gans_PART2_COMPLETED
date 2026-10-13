using System.Net;
using System.Text.Json;
using Xunit;

namespace RaceDay.Tests;

/// <summary>Proves the OpenAPI document Swagger UI is built from loads and lists every required endpoint.</summary>
public class SwaggerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public SwaggerTests(CustomWebApplicationFactory factory) { _factory = factory; }

    private async Task<JsonElement> GetPathsAsync()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("paths").Clone();
    }

    [Theory]
    // Auth
    [InlineData("/api/auth/register", "post")]
    [InlineData("/api/auth/login", "post")]
    [InlineData("/api/auth/logout", "post")]
    // Profile (Part 1 route and guide alias)
    [InlineData("/api/users/me", "get")]
    [InlineData("/api/users/me", "put")]
    [InlineData("/api/profile/me", "get")]
    [InlineData("/api/profile/me", "put")]
    // Events
    [InlineData("/api/events", "get")]
    [InlineData("/api/events", "post")]
    [InlineData("/api/events/{id}", "get")]
    [InlineData("/api/events/{id}", "put")]
    [InlineData("/api/events/{id}", "delete")]
    [InlineData("/api/events/mine", "get")]
    // Categories
    [InlineData("/api/events/{eventId}/categories", "get")]
    [InlineData("/api/events/{eventId}/categories", "post")]
    [InlineData("/api/categories/{id}", "put")]
    [InlineData("/api/categories/{id}", "delete")]
    // Enrolments (Part 1 routes and guide aliases)
    [InlineData("/api/categories/{categoryId}/enrol", "post")]
    [InlineData("/api/enrolments", "post")]
    [InlineData("/api/enrolments/me", "get")]
    [InlineData("/api/enrolments/mine", "get")]
    [InlineData("/api/events/{eventId}/enrolments", "get")]
    [InlineData("/api/enrolments/event/{eventId}", "get")]
    [InlineData("/api/enrolments/{id}/status", "put")]
    [InlineData("/api/enrolments/{id}", "delete")]
    // Results
    [InlineData("/api/enrolments/{enrolmentId}/results", "post")]
    [InlineData("/api/results", "post")]
    [InlineData("/api/participants/me/results", "get")]
    [InlineData("/api/results/mine", "get")]
    [InlineData("/api/results/event/{eventId}", "get")]
    [InlineData("/api/categories/{categoryId}/results", "get")]
    public async Task SwaggerDocument_ListsEveryRequiredEndpoint(string path, string method)
    {
        var paths = await GetPathsAsync();

        Assert.True(paths.TryGetProperty(path, out var pathItem), $"Swagger is missing path {path}");
        Assert.True(pathItem.TryGetProperty(method, out _), $"Swagger is missing {method.ToUpper()} {path}");
    }

    [Fact]
    public async Task SwaggerDocument_DocumentsTheErrorResponses_OnProtectedEndpoints()
    {
        var paths = await GetPathsAsync();
        var responses = paths.GetProperty("/api/events").GetProperty("post").GetProperty("responses");

        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("401", out _));
        Assert.True(responses.TryGetProperty("403", out _));
    }

    [Fact]
    public async Task SwaggerUi_Loads()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
