namespace Elsasa.Sis.Students.Features.Admission.WithdrawAdmissionApplication;

/// <summary>
/// Response payload returned after an admission application is withdrawn (ADR-005).
/// </summary>
public sealed record WithdrawAdmissionApplicationResponse(
    Guid Id,
    string Status,
    string? Notes = null,
    DateTimeOffset? UpdatedAt = null);
