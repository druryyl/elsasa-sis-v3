namespace Elsasa.Sis.Students.Features.Records.UpdateStudentIdentity;

/// <summary>
/// Request payload for updating certified student identity information (ADR-004, ADR-005).
/// </summary>
public sealed record UpdateStudentIdentityRequest(
    string FullName,
    string? PreferredName = null,
    DateOnly? DateOfBirth = null,
    string? PlaceOfBirth = null,
    string? Gender = null,
    string? Nationality = null,
    string? GovernmentIdType = null,
    string? GovernmentIdNumber = null,
    string? Address = null,
    string? Phone = null,
    string? Email = null,
    bool IsIndependent = false,
    string? Notes = null);
