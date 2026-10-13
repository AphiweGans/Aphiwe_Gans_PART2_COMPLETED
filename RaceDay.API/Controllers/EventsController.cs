using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public EventsController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>Lists all upcoming events (today onwards), soonest first. Public - no authentication required.</summary>
    /// <remarks>Past events are not listed here (Part 1 plan: "lists all upcoming events"); an Organiser still sees all of their own events with GET /api/events/mine, and any event can be opened by id.</remarks>
    /// <response code="200">List of upcoming events returned.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var today = DateTime.UtcNow.Date;

        var events = await _context.Events
            .Include(e => e.Categories)
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Returns a single event by id. Public - no authentication required.</summary>
    /// <response code="200">Event returned.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _context.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound();
        return Ok(ev);
    }

    /// <summary>Returns only the events owned by the logged-in Organiser. Organiser only.</summary>
    /// <response code="200">Events returned (may be an empty list).</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in as Participant, not Organiser.</response>
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var events = await _context.Events
            .Include(e => e.Categories)
            .Where(e => e.OrganiserID == organiserId)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Creates a new event. Organiser only.</summary>
    /// <param name="dto">Event name, date, location, description, distance, and event type.</param>
    /// <response code="201">Event created.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in as Participant, not Organiser.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventCreateDto dto)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ev = new Event
        {
            OrganiserID = organiserId,
            EventName = dto.EventName,
            EventDate = dto.EventDate,
            Location = dto.Location,
            Description = dto.Description,
            Distance = dto.Distance,
            EventType = dto.EventType
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, ev);
    }

    /// <summary>Updates an event. Organiser only, and only the organiser who owns the event.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the organiser who owns this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventCreateDto dto)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ev = await _context.Events.FindAsync(id);

        if (ev == null) return NotFound();
        if (ev.OrganiserID != organiserId) return Forbid();

        ev.EventName = dto.EventName;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Description = dto.Description;
        ev.Distance = dto.Distance;
        ev.EventType = dto.EventType;

        await _context.SaveChangesAsync();

        return Ok(ev);
    }

    /// <summary>Deletes an event. Organiser only, and only the organiser who owns the event.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the organiser who owns this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ev = await _context.Events.FindAsync(id);

        if (ev == null) return NotFound();
        if (ev.OrganiserID != organiserId) return Forbid();

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
