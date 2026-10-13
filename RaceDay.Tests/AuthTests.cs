using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;
using RaceDay.API.Services;
using Xunit;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public AuthTests(CustomWebApplicationFactory factory) { _factory = factory; }

    [Fact]
    public async Task Register_NewParticipant_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var dto = new RegisterDto { FullName = "Test Participant", Email = $"participant_{Guid.NewGuid()}@test.com", Password = "Password123!", Role = UserRole.Participant };
        var response = await client.PostAsJsonAsync("/api/auth/register", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_NewOrganiser_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var dto = new RegisterDto { FullName = "Test Organiser", Email = $"organiser_{Guid.NewGuid()}@test.com", Password = "Password123!", Role = UserRole.Organiser, Phone = "0821234567" };
        var response = await client.PostAsJsonAsync("/api/auth/register", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var email = $"dup_{Guid.NewGuid()}@test.com";
        var dto = new RegisterDto { FullName = "First User", Email = email, Password = "Password123!", Role = UserRole.Participant };
        await client.PostAsJsonAsync("/api/auth/register", dto);
        var secondAttempt = await client.PostAsJsonAsync("/api/auth/register", dto);
        Assert.Equal(HttpStatusCode.BadRequest, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmailAcrossRoles_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var email = $"cross_{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Organiser", Email = email, Password = "Password123!", Role = UserRole.Organiser });
        var second = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Participant", Email = email, Password = "Password123!", Role = UserRole.Participant });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_MissingEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "No Email", Email = "", Password = "Password123!", Role = UserRole.Participant });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Short Pw", Email = $"short_{Guid.NewGuid()}@test.com", Password = "abc", Role = UserRole.Participant });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var json = $"{{\"fullName\":\"Bad Role\",\"email\":\"badrole_{Guid.NewGuid()}@test.com\",\"password\":\"Password123!\",\"role\":99}}";
        var response = await client.PostAsync("/api/auth/register", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NeverPlainText()
    {
        var client = _factory.CreateClient();
        var email = $"hash_{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Hash Check", Email = email, Password = "Password123!", Role = UserRole.Participant });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
        var stored = db.Participants.Single(p => p.Email == email);

        Assert.NotEqual("Password123!", stored.PasswordHash);
        Assert.StartsWith("$2", stored.PasswordHash); // BCrypt hash prefix
        Assert.True(PasswordService.Verify("Password123!", stored.PasswordHash));
    }

    [Fact]
    public async Task Login_ValidOrganiserCredentials_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var email = $"login_{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Login User", Email = email, Password = "Password123!", Role = UserRole.Organiser });
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_ValidParticipantCredentials_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var email = $"loginp_{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Login Participant", Email = email, Password = "Password123!", Role = UserRole.Participant });
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"wrongpw_{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Wrong Password User", Email = email, Password = "Password123!", Role = UserRole.Organiser });
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = "IncorrectPassword!" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = $"nobody_{Guid.NewGuid()}@test.com", Password = "Password123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task FailedLogin_DoesNotCreateSession_ProtectedEndpointStillUnauthorized()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = "nobody@test.com", Password = "Password123!" });
        var response = await client.GetAsync("/api/profile/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive_AndTrimmed()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Mixed Case", Email = $"  Mixed.Case_{unique}@Test.com ", Password = "Password123!", Role = UserRole.Participant });

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = $"MIXED.case_{unique}@TEST.COM", Password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Register_SameEmailInDifferentCase_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "First", Email = $"case_{unique}@test.com", Password = "Password123!", Role = UserRole.Organiser });

        var second = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { FullName = "Second", Email = $"CASE_{unique}@TEST.com", Password = "Password123!", Role = UserRole.Participant });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithBlankPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = "someone@test.com", Password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
