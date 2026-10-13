using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

/// <summary>Proves invalid access is blocked: 401 when not logged in, 403 when logged in with the wrong role.</summary>
public class RoleAccessTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public RoleAccessTests(CustomWebApplicationFactory factory) { _factory = factory; }

    // ---- Not logged in -> 401 on every protected endpoint ----
    [Theory]
    [InlineData("GET", "/api/profile/me")]
    [InlineData("PUT", "/api/profile/me")]
    [InlineData("GET", "/api/events/mine")]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    [InlineData("POST", "/api/enrolments")]
    [InlineData("GET", "/api/enrolments/mine")]
    [InlineData("GET", "/api/enrolments/event/1")]
    [InlineData("PUT", "/api/enrolments/1/status")]
    [InlineData("DELETE", "/api/enrolments/1")]
    [InlineData("POST", "/api/results")]
    [InlineData("GET", "/api/results/mine")]
    [InlineData("GET", "/api/results/event/1")]
    // Part 1 plan routes
    [InlineData("GET", "/api/users/me")]
    [InlineData("PUT", "/api/users/me")]
    [InlineData("POST", "/api/categories/1/enrol")]
    [InlineData("GET", "/api/enrolments/me")]
    [InlineData("GET", "/api/events/1/enrolments")]
    [InlineData("POST", "/api/enrolments/1/results")]
    [InlineData("GET", "/api/participants/me/results")]
    public async Task ProtectedEndpoint_WithoutLogin_ReturnsUnauthorized(string method, string url)
    {
        var client = _factory.CreateClient();
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Participant hitting Organiser-only endpoints -> 403 ----
    [Theory]
    [InlineData("GET", "/api/events/mine")]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    [InlineData("GET", "/api/enrolments/event/1")]
    [InlineData("PUT", "/api/enrolments/1/status")]
    [InlineData("POST", "/api/results")]
    [InlineData("GET", "/api/results/event/1")]
    // Part 1 plan routes
    [InlineData("GET", "/api/events/1/enrolments")]
    [InlineData("POST", "/api/enrolments/1/results")]
    public async Task Participant_OnOrganiserOnlyEndpoint_ReturnsForbidden(string method, string url)
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- Organiser hitting Participant-only endpoints -> 403 ----
    [Theory]
    [InlineData("POST", "/api/enrolments")]
    [InlineData("GET", "/api/enrolments/mine")]
    [InlineData("DELETE", "/api/enrolments/1")]
    [InlineData("GET", "/api/results/mine")]
    // Part 1 plan routes
    [InlineData("POST", "/api/categories/1/enrol")]
    [InlineData("GET", "/api/enrolments/me")]
    [InlineData("GET", "/api/participants/me/results")]
    public async Task Organiser_OnParticipantOnlyEndpoint_ReturnsForbidden(string method, string url)
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInCategory_ReturnsForbidden()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var response = await organiser.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = category.CategoryID });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_ReturnsForbidden()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var ev = await (await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var response = await participant.PostAsJsonAsync($"/api/events/{ev!.EventID}/categories",
            new CategoryCreateDto { CategoryName = "5km", DistanceKm = 5, MaxParticipants = 100, EntryFee = 20 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- Public endpoints stay open to everyone ----
    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/categories/1/results")]
    public async Task PublicEndpoint_WithoutLogin_ReturnsOk(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
