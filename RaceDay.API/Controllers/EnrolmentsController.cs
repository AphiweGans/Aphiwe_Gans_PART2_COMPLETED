using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public EnrolmentsController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>Enters the logged-in Participant into an event by choosing a category (Part 1 route). Participant only.</summary>
    /// <remarks>No request body. The category belongs to the event being entered. The new enrolment starts as Confirmed (the Part 1 default) and an Organiser can change it with PUT /api/enrolments/{id}/status.</remarks>
    /// <param name="categoryId">The category to enter.</param>
    /// <response code="201">Enrolment created.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in as Organiser, not Participant.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Already enrolled in this category, or the category is full.</response>
    [HttpPost("api/categories/{categoryId:int}/enrol")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> EnrolInCategory(int categoryId) => EnrolAsync(categoryId);

    /// <summary>Enters the logged-in Participant into an event by choosing a category (body version). Participant only.</summary>
    /// <remarks>Input: categoryId in the body. Same behaviour as POST /api/categories/{categoryId}/enrol. The new enrolment starts as Confirmed.</remarks>
    /// <param name="dto">The category to enter.</param>
    /// <response code="201">Enrolment created.</response>
    /// <response code="400">Invalid or missing categoryId.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in as Organiser, not Participant.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Already enrolled in this category, or the category is full.</response>
    [HttpPost("api/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(EnrolmentCreateDto dto) => EnrolAsync(dto.CategoryId);

    /// <summary>Returns the logged-in Participant's own enrolments, including category and event. Participant only.</summary>
    /// <response code="200">Enrolments returned (may be empty).</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in as Organiser, not Participant.</response>
    [HttpGet("api/enrolments/me")]
    [HttpGet("api/enrolments/mine")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var enrolments = await _context.Enrolments
            .Include(en => en.Category)
            .ThenInclude(c => c!.Event)
            .Where(en => en.ParticipantID == participantId)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Returns all enrolments for an event. Organiser only, and only the Organiser who owns the event.</summary>
    /// <param name="eventId">The event whose enrolments should be listed.</param>
    /// <response code="200">Enrolments returned (may be empty).</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the Organiser who owns this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [HttpGet("api/enrolments/event/{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ev = await _context.Events.FindAsync(eventId);

        if (ev == null) return NotFound();
        if (ev.OrganiserID != organiserId) return Forbid();

        var enrolments = await _context.Enrolments
            .Where(en => en.Category!.EventID == eventId)
            .Select(en => new
            {
                en.EnrolmentID,
                en.Status,
                en.EnrolmentDate,
                en.CategoryID,
                CategoryName = en.Category!.CategoryName,
                Participant = new
                {
                    en.Participant!.ParticipantID,
                    en.Participant.FullName,
                    en.Participant.Email
                }
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Changes an enrolment's status (Pending, Confirmed or Cancelled). Organiser only, and only for their own events.</summary>
    /// <param name="id">The enrolment to update.</param>
    /// <param name="dto">The new status: Pending, Confirmed or Cancelled.</param>
    /// <response code="200">Status updated.</response>
    /// <response code="400">Status is not Pending, Confirmed or Cancelled.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the Organiser who owns the event this enrolment belongs to.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpPut("api/enrolments/{id:int}/status")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, EnrolmentStatusUpdateDto dto)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var status = EnrolmentStatuses.All
            .FirstOrDefault(s => string.Equals(s, dto.Status, StringComparison.OrdinalIgnoreCase));

        if (status == null)
        {
            return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });
        }

        var enrolment = await _context.Enrolments
            .Include(en => en.Category)
            .ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentID == id);

        if (enrolment == null || enrolment.Category?.Event == null) return NotFound();
        if (enrolment.Category.Event.OrganiserID != organiserId) return Forbid();

        enrolment.Status = status;
        await _context.SaveChangesAsync();

        return Ok(new { enrolment.EnrolmentID, enrolment.Status });
    }

    /// <summary>Withdraws (deletes) an enrolment. Participant only, and only their own enrolment.</summary>
    /// <param name="id">The enrolment to remove.</param>
    /// <response code="204">Enrolment removed.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the Participant who owns this enrolment.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var participantId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var enrolment = await _context.Enrolments.FindAsync(id);

        if (enrolment == null) return NotFound();
        if (enrolment.ParticipantID != participantId) return Forbid();

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<IActionResult> EnrolAsync(int categoryId)
    {
        var participantId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null) return NotFound(new { message = "Category not found." });

        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(en => en.ParticipantID == participantId && en.CategoryID == categoryId);

        if (alreadyEnrolled)
        {
            return Conflict(new { message = "Already enrolled in this category." });
        }

        var activeCount = await _context.Enrolments
            .CountAsync(en => en.CategoryID == categoryId && en.Status != EnrolmentStatuses.Cancelled);

        if (activeCount >= category.MaxParticipants)
        {
            return Conflict(new { message = "This category is full." });
        }

        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            CategoryID = categoryId,
            Status = EnrolmentStatuses.Confirmed
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return Created($"/api/enrolments/{enrolment.EnrolmentID}", enrolment);
    }
}
