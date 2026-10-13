using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class CategoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public CategoryTests(CustomWebApplicationFactory factory) { _factory = factory; }

    [Fact]
    public async Task Organiser_CanCreateCategory_OnOwnEvent_ReturnsCreated()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var ev = await (await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev!.EventID}/categories",
            new CategoryCreateDto { CategoryName = "21km", DistanceKm = 21, MaxParticipants = 200, EntryFee = 150 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AnyUser_CanViewCategoriesForEvent_ReturnsOk()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/events/{ev.EventID}/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await response.Content.ReadFromJsonAsync<List<Category>>();
        Assert.Contains(categories!, c => c.CategoryID == category.CategoryID);
    }

    [Fact]
    public async Task ViewCategories_ForMissingEvent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/events/999999/categories");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCreateCategory_OnAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var ev = await (await owner.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var response = await other.PostAsJsonAsync($"/api/events/{ev!.EventID}/categories",
            new CategoryCreateDto { CategoryName = "Hijack", DistanceKm = 5, MaxParticipants = 10, EntryFee = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnCategory_ReturnsOk()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryID}",
            new CategoryCreateDto { CategoryName = "Renamed", DistanceKm = 12, MaxParticipants = 50, EntryFee = 75 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<Category>();
        Assert.Equal("Renamed", updated!.CategoryName);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersCategory_ReturnsForbidden()
    {
        var owner = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var other = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/categories/{category.CategoryID}",
            new CategoryCreateDto { CategoryName = "Hijack", DistanceKm = 5, MaxParticipants = 10, EntryFee = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnCategory_ReturnsNoContent()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var response = await organiser.DeleteAsync($"/api/categories/{category.CategoryID}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteCategory_ReturnsForbidden()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var (_, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var update = await participant.PutAsJsonAsync($"/api/categories/{category.CategoryID}",
            new CategoryCreateDto { CategoryName = "X", DistanceKm = 5, MaxParticipants = 10, EntryFee = 0 });
        var delete = await participant.DeleteAsync($"/api/categories/{category.CategoryID}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidData_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var ev = await (await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventDto())).Content.ReadFromJsonAsync<Event>();

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev!.EventID}/categories",
            new CategoryCreateDto { CategoryName = "", DistanceKm = 5, MaxParticipants = 10, EntryFee = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
