namespace Elsasa.Sis.Contracts.Kpis;

/// <summary>
/// Cross-domain service contract that resolves active or selected operational period context,
/// automatic prior-semester comparison baselines, and selectable period options for KPI dashboards.
/// </summary>
public interface IKpiPeriodResolver
{
    /// <summary>
    /// Resolves the target academic year, operational period, optional prior comparison period,
    /// and normalized campus/school-level scope from the supplied global filter.
    /// </summary>
    /// <param name="filter">Incoming global KPI filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved KPI context.</returns>
    Task<ResolvedKpiContextDto> ResolveContextAsync(
        GlobalKpiFilterDto filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves selectable semester operational periods (<c>active</c> and <c>closed</c>)
    /// within the specified academic year, ordered chronologically.
    /// </summary>
    /// <param name="academicYearId">Academic year identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered list of selectable semester period options.</returns>
    Task<IReadOnlyList<KpiPeriodOptionDto>> GetAvailablePeriodsAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default);
}
