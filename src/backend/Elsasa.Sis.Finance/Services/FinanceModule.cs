namespace Elsasa.Sis.Finance.Services;

using Dapper;
using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Authoritative implementation of the synchronous Finance query contract (ADR-003, FIN-WF-13, GAP-006, TD-06).
/// Evaluates mandatory enrollment fee settlement in real time against the Finance ledger.
/// </summary>
public sealed class FinanceModule(ISqlConnectionFactory? connectionFactory = null) : IFinanceModule
{
    private readonly ISqlConnectionFactory? _connectionFactory = connectionFactory;

    static FinanceModule()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkEnrollmentFeeSettledSql = """
        SELECT 
            fo.id AS Id,
            fo.original_amount AS OriginalAmount,
            ISNULL(SUM(CASE WHEN pa.status = 'active' THEN pa.allocated_amount ELSE 0 END), 0) AS TotalAllocated
        FROM finance.financial_obligations fo
        LEFT JOIN finance.payment_allocations pa ON pa.obligation_id = fo.id
        LEFT JOIN operations.operational_periods op ON op.id = fo.academic_period_id
        WHERE (fo.applicant_id = @ApplicantId OR fo.student_id = (SELECT id FROM students.students WHERE applicant_id = @ApplicantId))
          AND fo.charge_type IN ('enrollment_fee', 'admission_fee')
          AND fo.is_mandatory = 1
          AND fo.status NOT IN ('cancelled')
          AND (fo.academic_period_id = @AcademicYearId OR op.academic_year_id = @AcademicYearId)
        GROUP BY fo.id, fo.original_amount;
        """;

    private sealed record ObligationSettlementRow(Guid Id, long OriginalAmount, long TotalAllocated);

    /// <inheritdoc />
    public async Task<Result<bool>> IsEnrollmentFeePaidAsync(
        Guid applicantId,
        Guid academicYearId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_connectionFactory is null)
        {
            throw new InvalidOperationException("ISqlConnectionFactory is not configured.");
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var obligations = (await connection.QueryAsync<ObligationSettlementRow>(
            new CommandDefinition(
                _checkEnrollmentFeeSettledSql,
                new { ApplicantId = applicantId, AcademicYearId = academicYearId },
                cancellationToken: ct))).ToList();

        // Case 1: No mandatory enrollment obligations assessed -> no gate block required
        if (obligations.Count == 0)
        {
            return Result.Success(true);
        }

        // Case 2: Return true if and only if EVERY mandatory enrollment obligation is 100% settled
        var allSettled = obligations.All(o => o.TotalAllocated >= o.OriginalAmount);

        return Result.Success(allSettled);
    }
}
