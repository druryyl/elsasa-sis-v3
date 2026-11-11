namespace Elsasa.Sis.Teaching.Features.Substitutions.ArrangeEmergencySubstitution;

/// <summary>
/// Command request for arranging an emergency substitution (TD-05, P4-S11, Resolution 3.8).
/// </summary>
/// <param name="OriginalAssignmentId">The assignment requiring substitute coverage.</param>
/// <param name="SubstituteTeacherId">The proposed candidate substitute teacher.</param>
/// <param name="StartDate">Start date of temporary coverage.</param>
/// <param name="EndDate">End date of temporary coverage.</param>
/// <param name="CoverageScopeType">Scope of coverage ('full' or 'partial').</param>
/// <param name="CoveredHoursPerWeek">Workload effort hours per week covered by substitute.</param>
/// <param name="Reason">Reason for emergency substitution (e.g., 'medical_leave', 'family_emergency', 'official_duty').</param>
/// <param name="WorkloadImpactHours">Alternative workload effort hours parameter.</param>
/// <param name="PartialScopeDetails">Optional description when coverage scope is partial.</param>
/// <param name="HandoverNotes">Optional instructional notes from primary teacher or coordinator.</param>
public sealed record ArrangeEmergencySubstitutionRequest(
    Guid OriginalAssignmentId,
    Guid SubstituteTeacherId,
    DateOnly StartDate,
    DateOnly EndDate,
    string CoverageScopeType,
    decimal? CoveredHoursPerWeek = null,
    string Reason = "",
    decimal? WorkloadImpactHours = null,
    string? PartialScopeDetails = null,
    string? HandoverNotes = null)
{
    /// <summary>
    /// Gets the resolved effective workload hours from either CoveredHoursPerWeek or WorkloadImpactHours.
    /// </summary>
    public decimal EffectiveHours => CoveredHoursPerWeek ?? WorkloadImpactHours ?? 0m;
}
