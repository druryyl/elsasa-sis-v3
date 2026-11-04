namespace Elsasa.Sis.Finance.Features.Accounts.Services;

using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Default implementation of <see cref="IStudentAccountService"/> providing idempotent account provisioning
/// and retrieval via parameterized Dapper queries.
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class StudentAccountService(ILogger<StudentAccountService> logger) : IStudentAccountService
{
    private const string _selectAccountSql = """
        SELECT
            id AS Id,
            account_number AS AccountNumber,
            student_id AS StudentId,
            account_status AS AccountStatus,
            opened_date AS OpenedDate
        FROM finance.student_financial_accounts
        WHERE student_id = @StudentId;
        """;

    private const string _upsertAccountSql = """
        IF NOT EXISTS (SELECT 1 FROM finance.student_financial_accounts WHERE student_id = @StudentId)
        BEGIN
            INSERT INTO finance.student_financial_accounts (
                id,
                account_number,
                student_id,
                currency_code,
                account_status,
                opened_date,
                created_at,
                created_by
            ) VALUES (
                @Id,
                @AccountNumber,
                @StudentId,
                'IDR',
                'active',
                @OpenedDate,
                SYSUTCDATETIME(),
                @CreatedBy
            );
        END;

        SELECT
            id AS Id,
            account_number AS AccountNumber,
            student_id AS StudentId,
            account_status AS AccountStatus,
            opened_date AS OpenedDate
        FROM finance.student_financial_accounts
        WHERE student_id = @StudentId;
        """;

    /// <inheritdoc />
    public async Task<StudentFinancialAccountSummary> GetOrCreateAccountAsync(
        Guid studentId,
        IDbConnection connection,
        IDbTransaction? transaction = null,
        string createdBy = "SYSTEM_ADHOC",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student ID cannot be empty.", nameof(studentId));
        }

        var existing = await connection.QuerySingleOrDefaultAsync<StudentFinancialAccountSummary>(
            new CommandDefinition(
                _selectAccountSql,
                new { StudentId = studentId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (existing is not null)
        {
            return existing;
        }

        var accountId = Guid.NewGuid();
        var accountNumber = GenerateAccountNumber(studentId);
        var openedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            var createdOrExisting = await connection.QuerySingleAsync<StudentFinancialAccountSummary>(
                new CommandDefinition(
                    _upsertAccountSql,
                    new
                    {
                        Id = accountId,
                        AccountNumber = accountNumber,
                        StudentId = studentId,
                        OpenedDate = openedDate,
                        CreatedBy = createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            logger.LogInformation(
                "Provisioned or retrieved financial account {AccountNumber} ({AccountId}) for student {StudentId}.",
                createdOrExisting.AccountNumber,
                createdOrExisting.Id,
                studentId);

            return createdOrExisting;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            logger.LogWarning(
                ex,
                "Concurrent race provisioning financial account for student {StudentId}; fetching established account.",
                studentId);

            var reloaded = await connection.QuerySingleAsync<StudentFinancialAccountSummary>(
                new CommandDefinition(
                    _selectAccountSql,
                    new { StudentId = studentId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            return reloaded;
        }
    }

    /// <summary>
    /// Generates the deterministic account number format: ACC-{Year}-{StudentIdPrefix8}.
    /// </summary>
    public static string GenerateAccountNumber(Guid studentId)
    {
        return $"ACC-{DateTime.UtcNow:yyyy}-{studentId.ToString()[..8].ToUpperInvariant()}";
    }
}
