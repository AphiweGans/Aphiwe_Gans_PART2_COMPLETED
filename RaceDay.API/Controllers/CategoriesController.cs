using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _context;

    public CategoriesController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>Lists categories for a specific event. Public - no authentication required.</summary>
    /// <response code="200">Categories returned.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("api/events/{eventId}/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventID == eventId);
        if (!eventExists) return NotFound();

        var categories = await _context.Categories.Where(c => c.EventID == eventId).ToListAsync();
        return Ok(categories);
    }

    /// <summary>Adds a category to an event. Organiser only, and only the organiser who owns the event.</summary>
    /// <param name="eventId">The event to add the category to.</param>
    /// <param name="dto">Category name, distance, max participants, and entry fee.</param>
    /// <response code="201">Category created.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the organiser who owns this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPost("api/events/{eventId}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryCreateDto dto)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ev = await _context.Events.FindAsync(eventId);

        if (ev == null) return NotFound();
        if (ev.OrganiserID != organiserId) return Forbid();

        var category = new Category
        {
            EventID = eventId,
            CategoryName = dto.CategoryName,
            DistanceKm = dto.DistanceKm,
            MaxParticipants = dto.MaxParticipants,
            EntryFee = dto.EntryFee
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForEvent), new { eventId }, category);
    }

    /// <summary>Updates a category. Organiser only, and only the organiser who owns the parent event.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the organiser who owns the parent event.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpPut("api/categories/{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryCreateDto dto)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category == null || category.Event == null) return NotFound();
        if (category.Event.OrganiserID != organiserId) return Forbid();

        category.CategoryName = dto.CategoryName;
        category.DistanceKm = dto.DistanceKm;
        category.MaxParticipants = dto.MaxParticipants;
        category.EntryFee = dto.EntryFee;

        await _context.SaveChangesAsync();

        return Ok(category);
    }

    /// <summary>Deletes a category. Organiser only, and only the organiser who owns the parent event.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the organiser who owns the parent event.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpDelete("api/categories/{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category == null || category.Event == null) return NotFound();
        if (category.Event.OrganiserID != organiserId) return Forbid();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
