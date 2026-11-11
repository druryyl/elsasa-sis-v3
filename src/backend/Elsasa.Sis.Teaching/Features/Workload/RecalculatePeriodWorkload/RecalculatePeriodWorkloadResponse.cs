namespace Elsasa.Sis.Teaching.Features.Workload.RecalculatePeriodWorkload;

/// <summary>
/// Response record returning recalculation outcome and synchronized snapshot counts (TD-04, P3-S09).
/// </summary>
/// <param name="AcademicPeriodId">The academic period identifier that was recalculated.</param>
/// <param name="RecalculatedCount">The number of teacher workload commitments reconciled or created.</param>
/// <param name="RecalculatedAt">UTC timestamp when recalculation was completed.</param>
/// <param name="Message">Human-readable summary message.</param>
public sealed record RecalculatePeriodWorkloadResponse(
    Guid AcademicPeriodId,
    int RecalculatedCount,
    DateTimeOffset RecalculatedAt,
    string Message);
