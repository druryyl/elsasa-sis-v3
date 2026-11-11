namespace Elsasa.Sis.Teaching.Features.Workload.RequestWorkloadException;

/// <summary>
/// Response payload returned after creating a formal workload exception recommendation (TD-04, P3-S10).
/// </summary>
/// <param name="Id">The unique identifier of the exception record.</param>
/// <param name="ExceptionNumber">Unique human-readable exception number (e.g. WEX-2026-0001).</param>
/// <param name="TeachingAssignmentId">Associated teaching assignment triggering the overload.</param>
/// <param name="TeacherProfileId">Target teacher profile identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="WorkloadPolicyId">Workload policy being departed from.</param>
/// <param name="ExceededHours">Requested overload hours.</param>
/// <param name="Justification">Coordinator rationale for exception.</param>
/// <param name="RecommendedBy">Coordinator username who submitted recommendation.</param>
/// <param name="RecommendedDate">Date the recommendation was recorded.</param>
/// <param name="EffectiveStartDate">Start date of exception validity.</param>
/// <param name="EffectiveEndDate">End date of exception validity.</param>
/// <param name="Status">Exception lifecycle status ('requested' or 'recommended').</param>
/// <param name="CreatedAt">Creation timestamp in UTC.</param>
public sealed record RequestWorkloadExceptionResponse(
    Guid Id,
    string ExceptionNumber,
    Guid TeachingAssignmentId,
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    Guid WorkloadPolicyId,
    decimal ExceededHours,
    string Justification,
    string RecommendedBy,
    DateOnly RecommendedDate,
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate,
    string Status,
    DateTimeOffset CreatedAt);
