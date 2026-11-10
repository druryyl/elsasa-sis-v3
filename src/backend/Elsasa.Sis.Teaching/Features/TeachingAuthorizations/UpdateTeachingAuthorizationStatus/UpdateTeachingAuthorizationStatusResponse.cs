namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.UpdateTeachingAuthorizationStatus;

/// <summary>
/// Response payload returned when a teaching authorization status has been transitioned (P4-S12).
/// </summary>
/// <param name="Id">The unique identifier of the teaching authorization.</param>
/// <param name="TeacherProfileId">The unique identifier of the owning teacher profile.</param>
/// <param name="AuthorizationNumber">The human-readable unique authorization identifier.</param>
/// <param name="PreviousStatus">The authorization status prior to this change.</param>
/// <param name="Status">The updated authorization status.</param>
/// <param name="StatusReason">The justification rationale for the status modification.</param>
/// <param name="UpdatedBy">The user identity who modified the authorization status.</param>
/// <param name="UpdatedAt">The timestamp when the status modification was recorded in UTC.</param>
public sealed record UpdateTeachingAuthorizationStatusResponse(
    Guid Id,
    Guid TeacherProfileId,
    string AuthorizationNumber,
    string PreviousStatus,
    string Status,
    string StatusReason,
    string UpdatedBy,
    DateTimeOffset UpdatedAt);
