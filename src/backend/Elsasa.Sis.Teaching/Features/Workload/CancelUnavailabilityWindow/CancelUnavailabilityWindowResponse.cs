namespace Elsasa.Sis.Teaching.Features.Workload.CancelUnavailabilityWindow;

/// <summary>
/// Response returned after successfully cancelling an unavailability window (P3-S08).
/// </summary>
/// <param name="Id">The unique identifier of the cancelled window.</param>
/// <param name="TeacherProfileId">The teacher profile identifier.</param>
/// <param name="Status">Updated status ('cancelled').</param>
/// <param name="CancelledAt">Timestamp when cancellation occurred in UTC.</param>
public sealed record CancelUnavailabilityWindowResponse(
    Guid Id,
    Guid TeacherProfileId,
    string Status,
    DateTimeOffset CancelledAt);
