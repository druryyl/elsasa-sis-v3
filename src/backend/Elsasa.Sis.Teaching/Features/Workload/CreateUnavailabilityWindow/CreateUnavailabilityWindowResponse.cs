namespace Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow;

/// <summary>
/// Response returned after successfully creating an unavailability window (P3-S08).
/// </summary>
/// <param name="Id">The unique identifier of the created window.</param>
/// <param name="TeacherProfileId">The affected teacher profile identifier.</param>
/// <param name="StartDate">Start date.</param>
/// <param name="EndDate">End date.</param>
/// <param name="UnavailabilityType">Leave category.</param>
/// <param name="Status">Status ('active').</param>
/// <param name="AffectedActiveAssignmentCount">Number of active assignments overlapping with this window (triggers coordinator awareness).</param>
/// <param name="CreatedAt">Creation timestamp in UTC.</param>
public sealed record CreateUnavailabilityWindowResponse(
    Guid Id,
    Guid TeacherProfileId,
    DateOnly StartDate,
    DateOnly EndDate,
    string UnavailabilityType,
    string Status,
    int AffectedActiveAssignmentCount,
    DateTimeOffset CreatedAt);
