using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.Contracts;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/users")]   // Part 1 plan: /api/users/me
[Route("api/profile")]  // Part 2 guide: /api/profile/me
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayContext _context;

    public ProfileController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>Returns the logged-in user's own profile (Organiser or Participant).</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">No active session.</response>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var (id, role) = GetCurrentUser();

        if (role == "Organiser")
        {
            var organiser = await _context.Organisers.FindAsync(id);
            if (organiser == null) return NotFound();

            return Ok(new { organiser.OrganiserID, organiser.FullName, organiser.Email, organiser.Phone, Role = "Organiser" });
        }
        else
        {
            var participant = await _context.Participants.FindAsync(id);
            if (participant == null) return NotFound();

            return Ok(new { participant.ParticipantID, participant.FullName, participant.Email, participant.DateOfBirth, participant.Gender, Role = "Participant" });
        }
    }

    /// <summary>Updates the logged-in user's own profile details.</summary>
    /// <param name="dto">Fields to update (FullName, Phone for Organisers, Gender for Participants).</param>
    /// <response code="200">Profile updated.</response>
    /// <response code="401">No active session.</response>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(ProfileUpdateDto dto)
    {
        var (id, role) = GetCurrentUser();

        if (role == "Organiser")
        {
            var organiser = await _context.Organisers.FindAsync(id);
            if (organiser == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.FullName)) organiser.FullName = dto.FullName;
            if (dto.Phone != null) organiser.Phone = dto.Phone;

            await _context.SaveChangesAsync();

            return Ok(new { organiser.OrganiserID, organiser.FullName, organiser.Email, organiser.Phone });
        }
        else
        {
            var participant = await _context.Participants.FindAsync(id);
            if (participant == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.FullName)) participant.FullName = dto.FullName;
            if (dto.Gender != null) participant.Gender = dto.Gender;

            await _context.SaveChangesAsync();

            return Ok(new { participant.ParticipantID, participant.FullName, participant.Email, participant.Gender });
        }
    }

    private (int id, string role) GetCurrentUser()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        return (id, role);
    }
}
