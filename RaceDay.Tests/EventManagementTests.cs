using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class EventManagementTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public EventManagementTests(CustomWebApplicationFactory factory) { _factory = factory; }

    [Fact]
    public async Task Organiser_CanCreateEvent_ReturnsCreated()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var eventDto = new EventCreateDto { EventName = "Test Marathon", EventDate = DateTime.UtcNow.AddMonths(1), Location = "Test City", Description = "Test event description", Distance = 42.2m, EventType = EventType.Run };
        var response = await client.PostAsJsonAsync("/api/events", eventDto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_ReturnsForbidden()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var eventDto = new EventCreateDto { EventName = "Unauthorized Event", EventDate = DateTime.UtcNow.AddMonths(1), Location = "Test City", Description = "Test event description", Distance = 10m, EventType = EventType.Run };
        var response = await client.PostAsJsonAsync("/api/events", eventDto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotCreateEvent_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var eventDto = new EventCreateDto { EventName = "No Auth Event", EventDate = DateTime.UtcNow.AddMonths(1), Location = "Test City", Description = "Test event description", Distance = 10m, EventType = EventType.Run };
        var response = await client.PostAsJsonAsync("/api/events", eventDto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnyUser_CanViewEvents_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewOwnEvents_OnlyReturnsEventsTheyOwn()
    {
        var ownerClient = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var otherClient = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);

        var ownedResponse = await ownerClient.PostAsJsonAsync("/api/events", new EventCreateDto
        {
            EventName = "My Own Event", EventDate = DateTime.UtcNow.AddMonths(1),
            Location = "Test City", Description = "Test event description", Distance = 10m, EventType = EventType.Run
        });
        var ownedEvent = await ownedResponse.Content.ReadFromJsonAsync<Event>();

        await otherClient.PostAsJsonAsync("/api/events", new EventCreateDto
        {
            EventName = "Someone Else's Event", EventDate = DateTime.UtcNow.AddMonths(1),
            Location = "Other City", Description = "Test event description", Distance = 5m, EventType = EventType.Walk
        });

        var mineResponse = await ownerClient.GetAsync("/api/events/mine");
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);

        var myEvents = await mineResponse.Content.ReadFromJsonAsync<List<Event>>();
        Assert.Contains(myEvents!, e => e.EventID == ownedEvent!.EventID);
        Assert.All(myEvents!, e => Assert.Equal(ownedEvent!.OrganiserID, e.OrganiserID));
    }

    [Fact]
    public async Task Participant_CannotViewEventsMine_ReturnsForbidden()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var response = await client.GetAsync("/api/events/mine");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_ReturnsForbidden()
    {
        var ownerClient = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var otherClient = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);

        var createResponse = await ownerClient.PostAsJsonAsync("/api/events", new EventCreateDto { EventName = "Owned Event", EventDate = DateTime.UtcNow.AddMonths(1), Location = "Test City", Description = "Test event description", Distance = 10m, EventType = EventType.Run });
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<Event>();

        var updateResponse = await otherClient.PutAsJsonAsync($"/api/events/{createdEvent!.EventID}", new EventCreateDto { EventName = "Hijacked Event", EventDate = DateTime.UtcNow.AddMonths(2), Location = "Different City", Description = "Test event description", Distance = 21.1m, EventType = EventType.Walk });
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }
}
