namespace Elsasa.Sis.IntegrationTests.Fixtures;

using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Shared.Common;

/// <summary>
/// Controllable test double for <see cref="IFinanceModule"/> in integration tests (ADR-003).
/// Allows tests to simulate paid and unpaid fee states.
/// </summary>
public sealed class TestFinanceModule : IFinanceModule
{
    private readonly HashSet<(Guid ApplicantId, Guid AcademicYearId)> _paidApplicants = [];
    private bool _alwaysApprove;

    /// <summary>
    /// Explicitly marks the enrollment fee as paid for an applicant in an academic year.
    /// </summary>
    public void MarkFeePaid(Guid applicantId, Guid academicYearId)
    {
        _paidApplicants.Add((applicantId, academicYearId));
    }

    /// <summary>
    /// Sets whether to approve all payment inquiries unconditionally.
    /// </summary>
    public void SetAlwaysApprove(bool approve)
    {
        _alwaysApprove = approve;
    }

    /// <summary>
    /// Resets the stub state to default unconfirmed behavior.
    /// </summary>
    public void Reset()
    {
        _paidApplicants.Clear();
        _alwaysApprove = false;
    }

    /// <inheritdoc />
    public Task<Result<bool>> IsEnrollmentFeePaidAsync(Guid applicantId, Guid academicYearId, CancellationToken ct = default)
    {
        if (_alwaysApprove || _paidApplicants.Contains((applicantId, academicYearId)))
        {
            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Success(false));
    }
}
