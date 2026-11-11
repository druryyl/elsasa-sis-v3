namespace Elsasa.Sis.Teaching.Features.Workload.RecalculatePeriodWorkload;

/// <summary>
/// Request record for triggering on-demand faculty workload recalculation for an academic period (TD-04, P3-S09).
/// </summary>
/// <param name="AcademicPeriodId">The academic period identifier whose commitments should be recalculated.</param>
public sealed record RecalculatePeriodWorkloadRequest(Guid AcademicPeriodId);
