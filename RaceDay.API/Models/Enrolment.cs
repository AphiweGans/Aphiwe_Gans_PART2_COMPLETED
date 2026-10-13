using RaceDay.Contracts;

namespace RaceDay.API.Models;

public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = EnrolmentStatuses.Confirmed;

    public Participant? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}
