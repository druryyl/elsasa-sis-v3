namespace Elsasa.Sis.Finance.Features.Accounts.Services;

using System.Data;

/// <summary>
/// Summary projection representing an active student financial account.
/// </summary>
/// <param name="Id">The primary key identifier of the financial account.</param>
/// <param name="AccountNumber">The unique human-readable account number (e.g., ACC-2026-XXXXXXXX).</param>
/// <param name="StudentId">The unique identifier of the owning student.</param>
/// <param name="AccountStatus">The lifecycle status of the account (active/closed).</param>
/// <param name="OpenedDate">The calendar date on which the account was opened.</param>
public sealed record StudentFinancialAccountSummary(
    Guid Id,
    string AccountNumber,
    Guid StudentId,
    string AccountStatus,
    DateOnly OpenedDate)
{
    /// <summary>
    /// Implicit conversion operator enabling seamless resolution to account UUID.
    /// </summary>
    public static implicit operator Guid(StudentFinancialAccountSummary account) => account.Id;
}

/// <summary>
/// Internal account provisioning service ensuring active student financial account records
/// are available for ledger obligations and assessment workflows (GAP-002, TD-03, ADR-003).
/// </summary>
public interface IStudentAccountService
{
    /// <summary>
    /// Retrieves an existing active account for the specified student, or idempotently provisions a new one.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="connection">The active database connection to execute within.</param>
    /// <param name="transaction">Optional ambient database transaction.</param>
    /// <param name="createdBy">Optional audit user tag (defaults to SYSTEM_ADHOC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly provisioned account summary.</returns>
    Task<StudentFinancialAccountSummary> GetOrCreateAccountAsync(
        Guid studentId,
        IDbConnection connection,
        IDbTransaction? transaction = null,
        string createdBy = "SYSTEM_ADHOC",
        CancellationToken cancellationToken = default);
}
