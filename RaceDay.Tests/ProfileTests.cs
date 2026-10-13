using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class ProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public ProfileTests(CustomWebApplicationFactory factory) { _factory = factory; }

    [Theory]
    [InlineData(UserRole.Organiser)]
    [InlineData(UserRole.Participant)]
    public async Task BothRoles_CanViewOwnProfile_ReturnsOk(UserRole role)
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, role);
        var response = await client.GetAsync("/api/profile/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotViewProfile_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/profile/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotUpdateProfile_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/profile/me", new ProfileUpdateDto { FullName = "Hacker" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnProfile_ChangesArePersisted()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Organiser);
        var update = await client.PutAsJsonAsync("/api/profile/me", new ProfileUpdateDto { FullName = "Updated Organiser", Phone = "0821112222" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile/me");
        Assert.Equal("Updated Organiser", profile.GetProperty("fullName").GetString());
        Assert.Equal("0821112222", profile.GetProperty("phone").GetString());
    }

    [Fact]
    public async Task Participant_CanUpdateOwnProfile_ChangesArePersisted()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var update = await client.PutAsJsonAsync("/api/profile/me", new ProfileUpdateDto { FullName = "Updated Participant", Gender = "Female" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile/me");
        Assert.Equal("Updated Participant", profile.GetProperty("fullName").GetString());
        Assert.Equal("Female", profile.GetProperty("gender").GetString());
    }

    [Fact]
    public async Task Profile_NeverExposesPasswordHash()
    {
        var client = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var body = await client.GetStringAsync("/api/profile/me");
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Users_OnlySeeTheirOwnProfile()
    {
        var a = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        var b = await TestHelpers.GetAuthenticatedClientAsync(_factory, UserRole.Participant);
        await a.PutAsJsonAsync("/api/profile/me", new ProfileUpdateDto { FullName = "Only Participant A" });

        var profileB = await b.GetFromJsonAsync<JsonElement>("/api/profile/me");
        Assert.NotEqual("Only Participant A", profileB.GetProperty("fullName").GetString());
    }
}
