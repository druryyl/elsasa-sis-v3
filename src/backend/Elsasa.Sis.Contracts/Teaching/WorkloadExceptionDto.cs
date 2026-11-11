namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing an authorized departure allowing a teacher to exceed the prohibited capacity threshold.
/// </summary>
/// <param name="Id">The unique identifier of the workload exception.</param>
/// <param name="ExceptionNumber">The authoritative unique exception identifier (e.g. 'WEX-2026-0012').</param>
/// <param name="TeachingAssignmentId">The assignment triggering the overload.</param>
/// <param name="TeacherProfileId">The teacher granted the exception.</param>
/// <param name="WorkloadPolicyId">The workload policy being departed from.</param>
/// <param name="ExceededHours">Number of hours exceeding the capacity threshold.</param>
/// <param name="Justification">Coordinator rationale justifying the exception.</param>
/// <param name="RecommendedBy">Coordinator who requested the exception.</param>
/// <param name="RecommendedDate">Date the exception was requested.</param>
/// <param name="ApprovedBy">Teaching Authority who approved the exception (self-approval prohibited).</param>
/// <param name="ApprovedDate">Date when approval was granted.</param>
/// <param name="EffectiveStartDate">Start date of exception validity.</param>
/// <param name="EffectiveEndDate">End date of exception validity.</param>
/// <param name="Status">Exception lifecycle status ('requested', 'recommended', 'approved', 'rejected').</param>
/// <param name="DecisionRemarks">Optional decision commentary from authority.</param>
/// <param name="CreatedAt">Timestamp when this exception record was created in UTC.</param>
public sealed record WorkloadExceptionDto(
    Guid Id,
    string ExceptionNumber,
    Guid TeachingAssignmentId,
    Guid TeacherProfileId,
    Guid WorkloadPolicyId,
    decimal ExceededHours,
    string Justification,
    string RecommendedBy,
    DateOnly RecommendedDate,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate,
    string Status,
    string? DecisionRemarks,
    DateTimeOffset CreatedAt);
