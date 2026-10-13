using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

// Shared request shapes (and the enums they use) for the RaceDay API.
// Kept in their own project so the API, the tests and the Part 3 MVC app
// all use exactly the same contracts.

public class RegisterDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>0 = Organiser, 1 = Participant.</summary>
    public UserRole Role { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ProfileUpdateDto
{
    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }
}

public class EventCreateDto
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be greater than 0 km.")]
    public decimal Distance { get; set; }

    /// <summary>0 = Run, 1 = Walk, 2 = Cycle.</summary>
    [EnumDataType(typeof(EventType))]
    public EventType EventType { get; set; }
}

public class CategoryCreateDto
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }
}

public class EnrolmentCreateDto
{
    /// <summary>The category (within an event) the participant wants to enter.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    public int CategoryId { get; set; }
}

public class EnrolmentStatusUpdateDto
{
    /// <summary>Pending, Confirmed or Cancelled.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Body for POST /api/enrolments/{enrolmentId}/results (Part 1 plan): finish time, position and status.</summary>
public class ResultCaptureDto
{
    /// <summary>Finish time as hh:mm:ss, e.g. 00:45:30.</summary>
    public TimeSpan? FinishTime { get; set; }

    /// <summary>Finishing position (1 = first).</summary>
    [Range(1, 100000)]
    public int? Position { get; set; }

    public string Status { get; set; } = "Finished";
}

/// <summary>Body for POST /api/results: the same as <see cref="ResultCaptureDto"/> plus the enrolment id.</summary>
public class ResultCreateDto : ResultCaptureDto
{
    /// <summary>The enrolment the result belongs to.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "EnrolmentId is required.")]
    public int EnrolmentId { get; set; }
}

// ---------------------------------------------------------------------------
// Response DTOs - the shapes the API sends back to clients (and that the
// Part 3 MVC app can deserialize into). They carry only what a client needs
// and never include password hashes.
// ---------------------------------------------------------------------------

public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>"Organiser" or "Participant".</summary>
    public string Role { get; set; } = string.Empty;
}

public class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public EventType EventType { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public DateTime EnrolmentDate { get; set; }

    /// <summary>Pending, Confirmed or Cancelled.</summary>
    public string Status { get; set; } = EnrolmentStatuses.Confirmed;
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Returned by the optional Part 3 image upload endpoints.</summary>
public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
}
