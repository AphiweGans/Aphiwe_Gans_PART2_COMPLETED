using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public ResultTests(CustomWebApplicationFactory factory) { _factory = factory; }

    private async Task<(HttpClient Organiser, HttpClient Participant, Event Event, Category Category, Enrolment Enrolment)> SetupAsync()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryID);
        return (organiser, participant, ev, category, enrolment);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_ForOwnEventEnrolment_ReturnsCreated()
    {
        var s = await SetupAsync();

        var response = await s.Organiser.PostAsJsonAsync("/api/results", new ResultCreateDto
        {
            EnrolmentId = s.Enrolment.EnrolmentID,
            FinishTime = TimeSpan.FromMinutes(45),
            Position = 1,
            Status = "Finished"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewOwnResults_AfterOrganiserCapturesThem()
    {
        var s = await SetupAsync();
        await s.Organiser.PostAsJsonAsync("/api/results", new ResultCreateDto { EnrolmentId = s.Enrolment.EnrolmentID, FinishTime = TimeSpan.FromMinutes(50), Position = 3, Status = "Finished" });

        var response = await s.Participant.GetAsync("/api/results/mine");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<List<Result>>();
        Assert.Single(results!);
        Assert.Equal(3, results![0].Position);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var s = await SetupAsync();
        var otherParticipant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        await s.Organiser.PostAsJsonAsync("/api/results", new ResultCreateDto { EnrolmentId = s.Enrolment.EnrolmentID, Position = 1, Status = "Finished" });

        var results = await otherParticipant.GetFromJsonAsync<List<Result>>("/api/results/mine");
        Assert.Empty(results!);
    }

    [Fact]
    public async Task CapturingSecondResultForSameEnrolment_ReturnsConflict()
    {
        var s = await SetupAsync();
        var dto = new ResultCreateDto { EnrolmentId = s.Enrolment.EnrolmentID, Position = 1, Status = "Finished" };

        await s.Organiser.PostAsJsonAsync("/api/results", dto);
        var second = await s.Organiser.PostAsJsonAsync("/api/results", dto);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResult_ForAnotherOrganisersEvent_ReturnsForbidden()
    {
        var s = await SetupAsync();
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);

        var response = await other.PostAsJsonAsync("/api/results", new ResultCreateDto { EnrolmentId = s.Enrolment.EnrolmentID, Position = 1, Status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureOwnResult_ReturnsForbidden()
    {
        var s = await SetupAsync();

        var response = await s.Participant.PostAsJsonAsync("/api/results", new ResultCreateDto { EnrolmentId = s.Enrolment.EnrolmentID, Position = 1, Status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForMissingEnrolment_ReturnsNotFound()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var response = await organiser.PostAsJsonAsync("/api/results", new ResultCreateDto { EnrolmentId = 999999, Position = 1, Status = "Finished" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithInvalidPosition_ReturnsBadRequest()
    {
        var s = await SetupAsync();
        var response = await s.Organiser.PostAsJsonAsync("/api/results", new ResultCreateDto { EnrolmentId = s.Enrolment.EnrolmentID, Position = 0, Status = "Finished" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewResultsForOwnEvent_ReturnsOk()
    {
        var s = await SetupAsync();
        await s.Organiser.PostAsJsonAsync("/api/results", new ResultCreateDto { EnrolmentId = s.Enrolment.EnrolmentID, Position = 1, Status = "Finished" });

        var response = await s.Organiser.GetAsync($"/api/results/event/{s.Event.EventID}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task Organiser_CannotViewResultsForAnotherOrganisersEvent_ReturnsForbidden()
    {
        var s = await SetupAsync();
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);

        var response = await other.GetAsync($"/api/results/event/{s.Event.EventID}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventResultsEndpoint_ReturnsForbidden()
    {
        var s = await SetupAsync();
        var response = await s.Participant.GetAsync($"/api/results/event/{s.Event.EventID}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnyUser_CanViewCategoryLeaderboard_WithoutLeakingPrivateData()
    {
        var s = await SetupAsync();
        await s.Organiser.PostAsJsonAsync("/api/results", new ResultCreateDto { EnrolmentId = s.Enrolment.EnrolmentID, Position = 1, Status = "Finished" });

        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/categories/{s.Category.CategoryID}/results");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", body, StringComparison.OrdinalIgnoreCase);
    }
}
