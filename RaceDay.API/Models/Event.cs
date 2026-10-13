using RaceDay.Contracts;

namespace RaceDay.API.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Distance { get; set; }
    public EventType EventType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organiser? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
