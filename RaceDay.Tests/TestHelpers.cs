using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.Tests;

public static class TestHelpers
{
    /// <summary>Registers a fresh user with the given role and logs them in. The returned client keeps the session cookie.</summary>
    public static async Task<HttpClient> GetAuthenticatedClientAsync(CustomWebApplicationFactory factory, UserRole role)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var email = $"{role}_{Guid.NewGuid()}@test.com";
        var registerDto = new RegisterDto
        {
            FullName = $"Test {role}",
            Email = email,
            Password = "Password123!",
            Role = role
        };

        await client.PostAsJsonAsync("/api/auth/register", registerDto);
        await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = "Password123!" });

        return client;
    }

    public static EventCreateDto NewEventDto(string name = "Test Event") => new()
    {
        EventName = name,
        EventDate = DateTime.UtcNow.AddMonths(1),
        Location = "Test City",
        Description = "A test event description",
        Distance = 10m,
        EventType = EventType.Run
    };

    /// <summary>Creates an event and one category as the given Organiser client.</summary>
    public static async Task<(Event Event, Category Category)> CreateEventWithCategoryAsync(
        HttpClient organiserClient, int maxParticipants = 100)
    {
        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", NewEventDto());
        var createdEvent = (await eventResponse.Content.ReadFromJsonAsync<Event>())!;

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{createdEvent.EventID}/categories",
            new CategoryCreateDto { CategoryName = "10km", DistanceKm = 10, MaxParticipants = maxParticipants, EntryFee = 50 });
        var createdCategory = (await categoryResponse.Content.ReadFromJsonAsync<Category>())!;

        return (createdEvent, createdCategory);
    }

    /// <summary>Enrols the Participant client in a category and returns the created enrolment.</summary>
    public static async Task<Enrolment> EnrolAsync(HttpClient participantClient, int categoryId)
    {
        var response = await participantClient.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
        return (await response.Content.ReadFromJsonAsync<Enrolment>())!;
    }
}
