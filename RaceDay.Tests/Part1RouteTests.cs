using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

/// <summary>
/// The Part 1 endpoint plan is the specification, so every route in it is tested exactly as written there.
/// (The routes from the Part 2 guide are covered in the other test classes.)
/// </summary>
public class Part1RouteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public Part1RouteTests(CustomWebApplicationFactory factory) { _factory = factory; }

    // ---- GET/PUT /api/users/me ----
    [Theory]
    [InlineData(UserRole.Organiser)]
    [InlineData(UserRole.Participant)]
    public async Task UsersMe_BothRoles_CanViewOwnProfile(UserRole role)
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, role);
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UsersMe_CanUpdateOwnProfile()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var update = await client.PutAsJsonAsync("/api/users/me", new ProfileUpdateDto { FullName = "Renamed", Phone = "0821234567" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/users/me");
        Assert.Equal("Renamed", profile.GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task UsersMe_PhoneLongerThanColumn_ReturnsBadRequest()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var update = await client.PutAsJsonAsync("/api/users/me", new ProfileUpdateDto { Phone = new string('1', 40) });
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    // ---- POST /api/categories/{id}/enrol ----
    [Fact]
    public async Task Enrol_ViaCategoryRoute_ReturnsCreated_AndShowsInEnrolmentsMe()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var enrol = await participant.PostAsync($"/api/categories/{category.CategoryID}/enrol", null);
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<Enrolment>>("/api/enrolments/me");
        Assert.Single(mine!);
        Assert.Equal(EnrolmentStatuses.Confirmed, mine![0].Status);
    }

    [Fact]
    public async Task Enrol_ViaCategoryRoute_Twice_ReturnsConflict()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        await participant.PostAsync($"/api/categories/{category.CategoryID}/enrol", null);
        var second = await participant.PostAsync($"/api/categories/{category.CategoryID}/enrol", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_ViaCategoryRoute_MissingCategory_ReturnsNotFound()
    {
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var response = await participant.PostAsync("/api/categories/999999/enrol", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- GET /api/events/{id}/enrolments ----
    [Fact]
    public async Task EventEnrolments_OwnerSeesThem_OtherOrganiserForbidden()
    {
        var owner = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(owner);
        await participant.PostAsync($"/api/categories/{category.CategoryID}/enrol", null);

        var ownerView = await owner.GetAsync($"/api/events/{ev.EventID}/enrolments");
        var otherView = await other.GetAsync($"/api/events/{ev.EventID}/enrolments");

        Assert.Equal(HttpStatusCode.OK, ownerView.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherView.StatusCode);
        Assert.Equal(1, (await ownerView.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
    }

    // ---- POST /api/enrolments/{id}/results, GET /api/participants/me/results ----
    [Fact]
    public async Task Results_Part1Flow_OrganiserCaptures_ParticipantViewsOwnHistory()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results",
            new ResultCaptureDto { FinishTime = TimeSpan.FromMinutes(52.5), Position = 3, Status = "Finished" });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var history = await participant.GetFromJsonAsync<List<Result>>("/api/participants/me/results");
        Assert.Single(history!);
        Assert.Equal(3, history![0].Position);
    }

    [Fact]
    public async Task Results_Part1Route_SecondResultForSameEnrolment_ReturnsConflict()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);
        var dto = new ResultCaptureDto { Position = 1, Status = "Finished" };

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", dto);
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", dto);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Results_Part1Route_OtherOrganiserAndParticipant_AreForbidden()
    {
        var owner = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(owner);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);
        var dto = new ResultCaptureDto { Position = 1, Status = "Finished" };

        var byOther = await other.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", dto);
        var byParticipant = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/results", dto);

        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byParticipant.StatusCode);
    }

    [Fact]
    public async Task Results_Part1Route_MissingEnrolment_ReturnsNotFound_AndBadPositionReturnsBadRequest()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);

        var missing = await organiser.PostAsJsonAsync("/api/enrolments/999999/results", new ResultCaptureDto { Position = 1 });
        var badPosition = await organiser.PostAsJsonAsync("/api/enrolments/1/results", new ResultCaptureDto { Position = 0 });

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badPosition.StatusCode);
    }

    // ---- GET /api/events lists UPCOMING events ----
    [Fact]
    public async Task Events_List_OnlyIncludesUpcomingEvents_ButPastEventStillOpensById()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);

        var upcomingDto = TestHelpers.NewEventDto("Upcoming Race");
        var pastDto = TestHelpers.NewEventDto("Past Race");
        pastDto.EventDate = DateTime.UtcNow.AddMonths(-2);

        var upcoming = await (await organiser.PostAsJsonAsync("/api/events", upcomingDto)).Content.ReadFromJsonAsync<Event>();
        var past = await (await organiser.PostAsJsonAsync("/api/events", pastDto)).Content.ReadFromJsonAsync<Event>();

        var list = await _factory.CreateClient().GetFromJsonAsync<List<Event>>("/api/events");
        Assert.Contains(list!, e => e.EventID == upcoming!.EventID);
        Assert.DoesNotContain(list!, e => e.EventID == past!.EventID);

        var mine = await organiser.GetFromJsonAsync<List<Event>>("/api/events/mine");
        Assert.Contains(mine!, e => e.EventID == past!.EventID);

        var byId = await _factory.CreateClient().GetAsync($"/api/events/{past!.EventID}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
    }
}
