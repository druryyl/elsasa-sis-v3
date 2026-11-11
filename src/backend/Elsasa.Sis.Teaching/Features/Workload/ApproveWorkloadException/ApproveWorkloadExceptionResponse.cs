namespace Elsasa.Sis.Teaching.Features.Workload.ApproveWorkloadException;

/// <summary>
/// Response payload returned after approving a workload exception (TD-04, P3-S10).
/// </summary>
/// <param name="Id">The unique identifier of the exception record.</param>
/// <param name="ExceptionNumber">Unique human-readable exception number (e.g. WEX-2026-0001).</param>
/// <param name="TeachingAssignmentId">Associated teaching assignment triggering the overload.</param>
/// <param name="TeacherProfileId">Target teacher profile identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="ExceededHours">Approved overload hours.</param>
/// <param name="Status">Exception lifecycle status ('approved').</param>
/// <param name="ApprovedBy">Teaching Authority username who approved the exception.</param>
/// <param name="ApprovedDate">Date when approval was granted.</param>
/// <param name="DecisionRemarks">Optional decision commentary recorded by authority.</param>
/// <param name="ApprovedExceptionCount">Updated count of approved exceptions for teacher in period.</param>
/// <param name="CommitmentStatus">Updated workload commitment status ('normal', 'approaching_limit', 'overloaded_with_exception', 'breached').</param>
/// <param name="ResolvedConflictsCount">Number of blocking capacity conflicts resolved/waived.</param>
/// <param name="ApprovedAt">Timestamp of approval in UTC.</param>
public sealed record ApproveWorkloadExceptionResponse(
    Guid Id,
    string ExceptionNumber,
    Guid TeachingAssignmentId,
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    decimal ExceededHours,
    string Status,
    string ApprovedBy,
    DateOnly ApprovedDate,
    string? DecisionRemarks,
    int ApprovedExceptionCount,
    string CommitmentStatus,
    int ResolvedConflictsCount,
    DateTimeOffset ApprovedAt);
