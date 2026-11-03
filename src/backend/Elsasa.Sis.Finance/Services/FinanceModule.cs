namespace Elsasa.Sis.Finance.Services;

using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Shared.Common;

/// <summary>
/// Stage 1 implementation of the synchronous Finance query contract (ADR-003, FIN-WF-13).
/// </summary>
/// <remarks>
/// The Finance payment ledger tables (<c>finance.financial_obligations</c>,
/// <c>finance.payments</c>, <c>finance.payment_allocations</c>) are not provisioned yet,
/// so enrollment fee confirmation is denied by default. Denying is the safe behaviour:
/// it keeps the enrollment payment gate closed instead of silently treating an
/// unverified payment as settled. Replace this implementation with a ledger-backed
/// query when the Finance payment slices are implemented.
/// </remarks>
public sealed class FinanceModule : IFinanceModule
{
    /// <inheritdoc />
    public Task<Result<bool>> IsEnrollmentFeePaidAsync(
        Guid applicantId,
        Guid academicYearId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // No Finance ledger exists yet, therefore payment can never be confirmed.
        return Task.FromResult(Result.Success(false));
    }
}
