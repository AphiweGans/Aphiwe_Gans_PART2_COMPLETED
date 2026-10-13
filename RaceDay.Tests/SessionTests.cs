using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

/// <summary>Proves the server-side session is created at login, identifies the user and role, and is cleared at logout.</summary>
public class SessionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public SessionTests(CustomWebApplicationFactory factory) { _factory = factory; }

    [Fact]
    public async Task Login_CreatesSession_ProtectedEndpointBecomesAccessible()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var response = await client.GetAsync("/api/profile/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Organiser, "Organiser")]
    [InlineData(UserRole.Participant, "Participant")]
    public async Task Session_IdentifiesTheLoggedInRole(UserRole role, string expectedRole)
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, role);
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile/me");
        Assert.Equal(expectedRole, profile.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Logout_ClearsSession_ProtectedEndpointReturnsUnauthorized()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var afterLogout = await client.GetAsync("/api/profile/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task TwoClients_HaveSeparateSessions()
    {
        var organiser = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var participant = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);

        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync("/api/events/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync("/api/events/mine")).StatusCode);
    }

    [Fact]
    public async Task LoginAsDifferentUser_ReplacesTheSession()
    {
        var client = _factory.CreateClient();
        var organiserEmail = $"swap_o_{Guid.NewGuid()}@test.com";
        var participantEmail = $"swap_p_{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "O", Email = organiserEmail, Password = "Password123!", Role = UserRole.Organiser });
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "P", Email = participantEmail, Password = "Password123!", Role = UserRole.Participant });

        await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = organiserEmail, Password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/events/mine")).StatusCode);

        await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = participantEmail, Password = "Password123!" });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/events/mine")).StatusCode);
    }
}
