namespace Elsasa.Sis.Academics.Features.CohortReview;

/// <summary>
/// Data transfer object representing a proposed academic standing determination for a student in a cohort review (TD-03, UC-ACD-17, P3-S06).
/// </summary>
public sealed record CohortStandingProposalDto(
    Guid StudentId,
    string? StudentNumber,
    string StudentName,
    string GradeLevel,
    int TotalUnits,
    int CompletedUnits,
    int FailedUnits,
    decimal AttendanceRate,
    string ProposedStanding,
    string PolicyReference,
    string? Conditions,
    string? ExistingStanding,
    DateTimeOffset? ExistingDeterminedAt,
    bool IsException = false,
    string? ExceptionReason = null,
    string? CommitteeReference = null,
    IReadOnlyList<string>? FailingUnitNames = null);
