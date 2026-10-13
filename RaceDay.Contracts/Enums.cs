namespace RaceDay.Contracts;

public enum UserRole
{
    Organiser,
    Participant
}

public enum EventType
{
    Run,
    Walk,
    Cycle
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Pending, Confirmed, Cancelled };
}
