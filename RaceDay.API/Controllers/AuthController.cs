using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _context;

    public AuthController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>Registers a new user as either an Organiser or a Participant.</summary>
    /// <remarks>
    /// Input: fullName, email, password (min 8 characters) and role (0 = Organiser, 1 = Participant).
    /// Organisers may also send phone; Participants may also send dateOfBirth and gender.
    /// The password is hashed with BCrypt before saving - the original is never stored.
    /// </remarks>
    /// <param name="dto">Full name, email, password, and role.</param>
    /// <response code="201">Account created successfully.</response>
    /// <response code="400">Missing/invalid fields, invalid role, or an account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "FullName, Email, and Password are required." });
        }

        if (!Enum.IsDefined(typeof(UserRole), dto.Role))
        {
            return BadRequest(new { message = "Role must be Organiser (0) or Participant (1)." });
        }

        // Emails are trimmed and lower-cased so "Jane@Test.com" and "jane@test.com" are the same account
        // (SQL Server is case-insensitive by default, so this keeps every provider consistent).
        var email = dto.Email.Trim().ToLowerInvariant();
        var fullName = dto.FullName.Trim();

        var emailInUse = await _context.Organisers.AnyAsync(o => o.Email == email)
                          || await _context.Participants.AnyAsync(p => p.Email == email);

        if (emailInUse)
        {
            return BadRequest(new { message = "An account with this email already exists." });
        }

        var hash = PasswordService.Hash(dto.Password);

        if (dto.Role == UserRole.Organiser)
        {
            var organiser = new Organiser
            {
                FullName = fullName,
                Email = email,
                PasswordHash = hash,
                Phone = dto.Phone
            };

            _context.Organisers.Add(organiser);
            await _context.SaveChangesAsync();

            return Created(string.Empty, new { organiser.OrganiserID, organiser.FullName, organiser.Email, Role = "Organiser" });
        }
        else
        {
            var participant = new Participant
            {
                FullName = fullName,
                Email = email,
                PasswordHash = hash,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            return Created(string.Empty, new { participant.ParticipantID, participant.FullName, participant.Email, Role = "Participant" });
        }
    }

    /// <summary>Authenticates a user and creates a server-side session storing their UserId and Role.</summary>
    /// <remarks>Input: the registered email and password. On success the response also tells you which role you are logged in as.</remarks>
    /// <param name="dto">Registered email and password.</param>
    /// <response code="200">Login successful; session created.</response>
    /// <response code="400">Email or password missing.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Email and Password are required." });
        }

        var email = dto.Email.Trim().ToLowerInvariant();

        var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (organiser != null && PasswordService.Verify(dto.Password, organiser.PasswordHash))
        {
            StartSession(organiser.OrganiserID, organiser.FullName, UserRole.Organiser);
            return Ok(new { organiser.OrganiserID, organiser.FullName, Role = "Organiser" });
        }

        var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email == email);
        if (participant != null && PasswordService.Verify(dto.Password, participant.PasswordHash))
        {
            StartSession(participant.ParticipantID, participant.FullName, UserRole.Participant);
            return Ok(new { participant.ParticipantID, participant.FullName, Role = "Participant" });
        }

        return Unauthorized(new { message = "Invalid email or password." });
    }

    /// <summary>Ends the current session so protected endpoints are no longer accessible.</summary>
    /// <response code="200">Logged out successfully.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }

    // Stores the user's id and role in the SERVER-SIDE session. Later requests are
    // identified by the session-id cookie; SessionAuthenticationHandler reads these
    // values back to enforce roles.
    private void StartSession(int userId, string fullName, UserRole role)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
        HttpContext.Session.SetString(SessionKeys.Role, role.ToString());
        HttpContext.Session.SetString(SessionKeys.FullName, fullName);
    }
}
