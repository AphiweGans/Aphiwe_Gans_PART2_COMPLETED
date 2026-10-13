using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RaceDay.API.Services;

/// <summary>Keys used to store the logged-in user's details in the server-side session.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
}

/// <summary>
/// Authentication handler that trusts ONLY the server-side session (held in server
/// memory, identified by an opaque session-id cookie). At login the API stores
/// UserId and Role in that session; on every later request this handler reads them
/// back and builds the ClaimsPrincipal, so [Authorize(Roles = "...")] works as normal.
/// Nothing about the user's identity or role is stored in the browser.
/// </summary>
public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Session";

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Context.Session.LoadAsync();

        var userId = Context.Session.GetInt32(SessionKeys.UserId);
        var role = Context.Session.GetString(SessionKeys.Role);

        if (userId == null || string.IsNullOrEmpty(role))
        {
            return AuthenticateResult.NoResult();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new Claim(ClaimTypes.Name, Context.Session.GetString(SessionKeys.FullName) ?? string.Empty),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    // Not logged in -> 401 (an API should never redirect to a login page).
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    // Logged in but wrong role -> 403.
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
