using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public EnrolmentTests(CustomWebApplicationFactory factory) { _factory = factory; }

    [Fact]
    public async Task Participant_CanEnrolInCategory_AndEnrolmentIsRecordedCorrectly()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var enrolResponse = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = category.CategoryID });
        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);

        var mine = await participant.GetAsync("/api/enrolments/mine");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);

        var enrolments = await mine.Content.ReadFromJsonAsync<List<Enrolment>>();
        Assert.Single(enrolments!);
        Assert.Equal(category.CategoryID, enrolments![0].CategoryID);
        Assert.Equal(EnrolmentStatuses.Confirmed, enrolments[0].Status); // Part 1 default
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameCategory_ReturnsConflict()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        await TestHelpers.EnrolAsync(participant, category.CategoryID);
        var second = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = category.CategoryID });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotEnrolInFullCategory_ReturnsConflict()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var first = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var second = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser, maxParticipants: 1);

        var firstResponse = await first.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = category.CategoryID });
        var secondResponse = await second.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = category.CategoryID });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolInMissingCategory_ReturnsNotFound()
    {
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var response = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = 999999 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolWithoutCategoryId_ReturnsBadRequest()
    {
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var response = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotEnrol_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent_WithoutLeakingPasswordHashes()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var response = await organiser.GetAsync($"/api/enrolments/event/{ev.EventID}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);

        var list = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsForAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var (ev, _) = await TestHelpers.CreateEventWithCategoryAsync(owner);

        var response = await other.GetAsync($"/api/enrolments/event/{ev.EventID}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewEnrolmentsForMissingEvent_ReturnsNotFound()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var response = await organiser.GetAsync("/api/enrolments/event/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesOwnEnrolments()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var a = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var b = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        await TestHelpers.EnrolAsync(a, category.CategoryID);

        var bEnrolments = await b.GetFromJsonAsync<List<Enrolment>>("/api/enrolments/mine");
        Assert.Empty(bEnrolments!);
    }

    [Fact]
    public async Task Participant_CanWithdrawOwnEnrolment_ReturnsNoContent()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentID}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotWithdrawSomeoneElsesEnrolment_ReturnsForbidden()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var owner = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(owner, category.CategoryID);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentID}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Pending")]
    [InlineData("confirmed")] // case-insensitive
    public async Task Organiser_CanChangeEnrolmentStatus_ForOwnEvent_ReturnsOkAndPersists(string newStatus)
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/status",
            new EnrolmentStatusUpdateDto { Status = newStatus });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<Enrolment>>("/api/enrolments/mine");
        Assert.Equal(newStatus, mine![0].Status, ignoreCase: true);
    }

    [Fact]
    public async Task Organiser_UpdateEnrolmentStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/status",
            new EnrolmentStatusUpdateDto { Status = "Banana" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotChangeStatusOfAnotherOrganisersEnrolment_ReturnsForbidden()
    {
        var owner = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(owner);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var response = await other.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/status",
            new EnrolmentStatusUpdateDto { Status = "Confirmed" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotChangeEnrolmentStatus_ReturnsForbidden()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);

        var response = await participant.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentID}/status",
            new EnrolmentStatusUpdateDto { Status = "Confirmed" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
