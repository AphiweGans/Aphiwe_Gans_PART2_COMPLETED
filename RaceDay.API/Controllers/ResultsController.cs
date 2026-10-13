using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public ResultsController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>Captures a finish time and finishing position for an enrolment (Part 1 route). Organiser only, and only for their own events.</summary>
    /// <remarks>Input: finishTime (hh:mm:ss), position (1 = first) and status (e.g. Finished). An enrolment can only have one result.</remarks>
    /// <param name="enrolmentId">The enrolment the result belongs to.</param>
    /// <param name="dto">Finish time, position and status.</param>
    /// <response code="201">Result created.</response>
    /// <response code="400">Invalid input (e.g. position below 1).</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the Organiser who owns the event (Participants can never capture results).</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> CaptureForEnrolment(int enrolmentId, ResultCaptureDto dto) => CaptureAsync(enrolmentId, dto);

    /// <summary>Captures a finish time and finishing position for an enrolment (body version). Organiser only, and only for their own events.</summary>
    /// <remarks>Input: enrolmentId, finishTime (hh:mm:ss), position (1 = first) and status. Same behaviour as POST /api/enrolments/{enrolmentId}/results.</remarks>
    /// <param name="dto">Enrolment id, finish time, position and status.</param>
    /// <response code="201">Result created.</response>
    /// <response code="400">Invalid input (e.g. position below 1).</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the Organiser who owns the event (Participants can never capture results).</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("api/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(ResultCreateDto dto) => CaptureAsync(dto.EnrolmentId, dto);

    /// <summary>Returns the logged-in Participant's own race results, including the event. Participant only.</summary>
    /// <response code="200">Results returned (may be empty).</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in as Organiser, not Participant.</response>
    [HttpGet("api/participants/me/results")]
    [HttpGet("api/results/mine")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var results = await _context.Results
            .Include(r => r.Enrolment)
            .ThenInclude(en => en!.Category)
            .ThenInclude(c => c!.Event)
            .Where(r => r.Enrolment!.ParticipantID == participantId)
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Returns every captured result for an event. Organiser only, and only the Organiser who owns the event.</summary>
    /// <param name="eventId">The event whose results should be listed.</param>
    /// <response code="200">Results returned, ordered by position (may be empty).</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the Organiser who owns this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("api/results/event/{eventId:int}")]
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

        var results = await _context.Results
            .Where(r => r.Enrolment!.Category!.EventID == eventId)
            .OrderBy(r => r.Position)
            .Select(r => new
            {
                r.ResultID,
                r.EnrolmentID,
                r.FinishTime,
                r.Position,
                r.Status,
                CategoryName = r.Enrolment!.Category!.CategoryName,
                ParticipantName = r.Enrolment.Participant!.FullName
            })
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Returns the public results leaderboard for a category (names and positions only). No authentication required.</summary>
    /// <param name="categoryId">The category whose leaderboard should be returned.</param>
    /// <response code="200">Leaderboard returned (may be empty).</response>
    [HttpGet("api/categories/{categoryId:int}/results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForCategory(int categoryId)
    {
        var results = await _context.Results
            .Where(r => r.Enrolment!.CategoryID == categoryId)
            .OrderBy(r => r.Position)
            .Select(r => new
            {
                r.Position,
                r.FinishTime,
                r.Status,
                ParticipantName = r.Enrolment!.Participant!.FullName
            })
            .ToListAsync();

        return Ok(results);
    }

    private async Task<IActionResult> CaptureAsync(int enrolmentId, ResultCaptureDto dto)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var enrolment = await _context.Enrolments
            .Include(en => en.Category)
            .ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentID == enrolmentId);

        if (enrolment == null || enrolment.Category?.Event == null) return NotFound();
        if (enrolment.Category.Event.OrganiserID != organiserId) return Forbid();

        var existing = await _context.Results.AnyAsync(r => r.EnrolmentID == enrolmentId);
        if (existing) return Conflict(new { message = "A result already exists for this enrolment." });

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Finished" : dto.Status
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return Created($"/api/results/{result.ResultID}", result);
    }
}
