namespace Elsasa.Sis.Teaching.Features.Kpis.GetTeachingKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Query handler computing the Level 2 Teaching KPI Dashboard payload (<c>TCH-01</c>.. <c>TCH-04</c>,
/// diagnostic breakdowns, and active attention signals).
/// </summary>
public sealed class GetTeachingKpiDashboardHandler(
    ISqlConnectionFactory connectionFactory,
    IKpiPeriodResolver periodResolver)
{
    /// <summary>
    /// Computes the Teaching KPI dashboard for the supplied global filter criteria.
    /// </summary>
    /// <param name="filter">Incoming period, campus, and school-level filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Teaching domain KPI dashboard response wrapped in <see cref="Result{TValue}"/>.</returns>
    public async Task<Result<DomainKpiDashboardResponse>> HandleAsync(
        GlobalKpiFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var context = await periodResolver.ResolveContextAsync(filter, cancellationToken);
        var availablePeriods = await periodResolver.GetAvailablePeriodsAsync(context.AcademicYearId, cancellationToken);
        var snapshot = await TeachingKpiQueries.QueryAsync(connectionFactory, context, cancellationToken);

        var response = new DomainKpiDashboardResponse(
            DomainKey: "teaching",
            DomainName: "Teaching",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Context: context,
            AvailablePeriods: availablePeriods,
            HeadlineKpis: snapshot.HeadlineKpis,
            Breakdowns: snapshot.Breakdowns,
            AttentionSignals: snapshot.AttentionSignals);

        return Result.Success(response);
    }
}
