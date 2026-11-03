namespace Elsasa.Sis.Students.Features.Records.SearchStudents;

/// <summary>
/// Lightweight student summary card DTO for list and search projections (SCR-STU-02).
/// </summary>
public sealed record StudentListItemDto(
    Guid Id,
    string StudentNumber,
    string FullName,
    string? PreferredName,
    string CurrentStatus,
    DateOnly EnrollmentDate,
    string InitialCampus,
    string InitialLevel,
    bool IsIndependent,
    string? PrimaryGuardianName,
    string? PrimaryGuardianPhone);
