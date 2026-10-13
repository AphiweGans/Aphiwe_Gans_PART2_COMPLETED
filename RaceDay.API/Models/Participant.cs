using System.Text.Json.Serialization;

namespace RaceDay.API.Models;

public class Participant
{
    public int ParticipantID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
