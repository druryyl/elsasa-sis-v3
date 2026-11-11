namespace Elsasa.Sis.Finance.Features.Kpis.GetFinanceKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Query handler for the Level 2 Finance KPI Dashboard (<c>GET /api/finance/kpis/dashboard</c>).
/// Resolves period context and computes <c>FIN-01</c> through <c>FIN-05</c>, aging distribution,
/// and finance attention signals from <c>finance.v_student_receivables</c> and <c>finance.payments</c>.
/// </summary>
public sealed class GetFinanceKpiDashboardHandler(
    ISqlConnectionFactory connectionFactory,
    IKpiPeriodResolver periodResolver)
{
    /// <summary>
    /// Computes the Finance domain KPI dashboard response for the specified global filter.
    /// </summary>
    /// <param name="filter">Global KPI filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The computed Finance domain KPI dashboard response.</returns>
    public async Task<Result<DomainKpiDashboardResponse>> HandleAsync(
        GlobalKpiFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var context = await periodResolver.ResolveContextAsync(filter, cancellationToken);
        var availablePeriods = await periodResolver.GetAvailablePeriodsAsync(context.AcademicYearId, cancellationToken);
        var snapshot = await FinanceKpiMetricsCalculator.ComputeAsync(connectionFactory, context, cancellationToken);

        var response = new DomainKpiDashboardResponse(
            DomainKey: "finance",
            DomainName: "Finance",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Context: context,
            AvailablePeriods: availablePeriods,
            HeadlineKpis: snapshot.HeadlineKpis,
            Breakdowns: snapshot.Breakdowns,
            AttentionSignals: snapshot.AttentionSignals);

        return Result.Success(response);
    }
}
