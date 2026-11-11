namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a temporary leave, official training, illness, or blackout window
/// restricting a teacher's availability.
/// </summary>
/// <param name="Id">The unique identifier of the unavailability window.</param>
/// <param name="TeacherProfileId">The affected teacher profile identifier.</param>
/// <param name="StartDate">Start date of the leave/blackout window.</param>
/// <param name="EndDate">End date of the leave/blackout window.</param>
/// <param name="StartTime">Optional daily start time for partial day restrictions.</param>
/// <param name="EndTime">Optional daily end time for partial day restrictions.</param>
/// <param name="UnavailabilityType">Leave category ('medical_leave', 'official_training', 'personal_leave', 'blackout').</param>
/// <param name="ExternalSourceReference">Optional reference to HR leave application or external tracking number.</param>
/// <param name="Reason">Detailed justification or medical leave explanation.</param>
/// <param name="Status">Lifecycle status ('active', 'cancelled', 'expired').</param>
/// <param name="Notes">Optional coordinator notes.</param>
/// <param name="CreatedAt">Timestamp when this leave window was recorded in UTC.</param>
public sealed record TeacherUnavailabilityWindowDto(
    Guid Id,
    Guid TeacherProfileId,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string UnavailabilityType,
    string? ExternalSourceReference,
    string Reason,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt);
