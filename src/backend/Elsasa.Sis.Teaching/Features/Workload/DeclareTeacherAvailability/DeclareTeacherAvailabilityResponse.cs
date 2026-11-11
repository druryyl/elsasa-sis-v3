namespace Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability;

/// <summary>
/// Response returned after successfully recording or superseding teacher availability (P3-S08).
/// </summary>
/// <param name="Id">The unique identifier of the created availability record.</param>
/// <param name="TeacherProfileId">The teacher profile identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="MaxAvailableHours">Declared maximum available hours.</param>
/// <param name="Status">Status of the record ('active').</param>
/// <param name="SupersededPreviousCount">Number of previous active records superseded.</param>
/// <param name="CreatedAt">Creation timestamp in UTC.</param>
public sealed record DeclareTeacherAvailabilityResponse(
    Guid Id,
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    decimal MaxAvailableHours,
    string Status,
    int SupersededPreviousCount,
    DateTimeOffset CreatedAt);
