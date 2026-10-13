using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class EventCrudTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public EventCrudTests(CustomWebApplicationFactory factory) { _factory = factory; }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_ReturnsOkAndPersists()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var created = await (await client.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto("Before"))).Content.ReadFromJsonAsync<Event>();

        var update = TestHelpers.NewEventDto("After");
        var response = await client.PutAsJsonAsync($"/api/events/{created!.EventID}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<Event>($"/api/events/{created.EventID}");
        Assert.Equal("After", fetched!.EventName);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_ThenItIsGone()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var created = await (await client.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var delete = await client.DeleteAsync($"/api/events/{created!.EventID}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await client.GetAsync($"/api/events/{created.EventID}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var created = await (await owner.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var response = await other.DeleteAsync($"/api/events/{created!.EventID}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateEvent_ReturnsForbidden()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var created = await (await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var response = await participant.PutAsJsonAsync($"/api/events/{created!.EventID}", TestHelpers.NewEventDto("Hijack"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_ReturnsForbidden()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var created = await (await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var response = await participant.DeleteAsync($"/api/events/{created!.EventID}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ThatDoesNotExist_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ThatDoesNotExist_ReturnsNotFound()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var response = await client.PutAsJsonAsync("/api/events/999999", TestHelpers.NewEventDto());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewSingleEvent_ReturnsOk()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var created = await (await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var response = await participant.GetAsync($"/api/events/{created!.EventID}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingDescription_ReturnsBadRequest()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var dto = TestHelpers.NewEventDto();
        dto.Description = "";
        var response = await client.PostAsJsonAsync("/api/events", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_ZeroDistance_ReturnsBadRequest()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var dto = TestHelpers.NewEventDto();
        dto.Distance = 0m;
        var response = await client.PostAsJsonAsync("/api/events", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
