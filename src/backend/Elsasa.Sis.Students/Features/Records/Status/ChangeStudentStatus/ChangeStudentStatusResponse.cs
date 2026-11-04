namespace Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;

/// <summary>
/// Response payload returned upon successful student status mutation (GAP-005, TD-05).
/// </summary>
/// <param name="StatusId">The unique identifier of the newly inserted student status period.</param>
/// <param name="StudentId">The unique student identifier.</param>
/// <param name="Status">The new operational status value.</param>
/// <param name="IsCurrent">Indicates whether the newly inserted period is active.</param>
public sealed record ChangeStudentStatusResponse(
    Guid StatusId,
    Guid StudentId,
    string Status,
    bool IsCurrent);
