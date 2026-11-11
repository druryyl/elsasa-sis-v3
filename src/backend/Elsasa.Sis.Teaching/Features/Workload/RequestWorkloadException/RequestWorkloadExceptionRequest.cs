namespace Elsasa.Sis.Teaching.Features.Workload.RequestWorkloadException;

/// <summary>
/// Request payload to submit a formal workload capacity exception recommendation (TD-04, P3-S10).
/// </summary>
/// <param name="TeacherProfileId">The teacher profile to be granted the exception.</param>
/// <param name="AcademicPeriodId">The academic period governing the exception.</param>
/// <param name="TeachingAssignmentId">Optional assignment triggering the overload.</param>
/// <param name="RequestedOverloadHours">The number of hours requested to exceed the capacity threshold.</param>
/// <param name="CoordinatorJustification">Coordinator rationale justifying the overload exception.</param>
/// <param name="EffectiveStartDate">Optional effective start date of exception validity.</param>
/// <param name="EffectiveEndDate">Optional effective end date of exception validity.</param>
/// <param name="Status">Optional initial status ('requested' or 'recommended', defaults to 'requested').</param>
public sealed record RequestWorkloadExceptionRequest(
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    Guid? TeachingAssignmentId,
    decimal RequestedOverloadHours,
    string CoordinatorJustification,
    DateOnly? EffectiveStartDate = null,
    DateOnly? EffectiveEndDate = null,
    string? Status = "requested");
