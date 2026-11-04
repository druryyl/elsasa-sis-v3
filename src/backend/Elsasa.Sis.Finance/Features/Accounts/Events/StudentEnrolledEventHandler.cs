namespace Elsasa.Sis.Finance.Features.Accounts.Events;

using Dapper;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Asynchronously handles <see cref="StudentEnrolledEvent"/> to graduate pre-enrollment accounts or provision active
/// student financial accounts in <c>finance.student_financial_accounts</c> upon student enrollment (ADR-003, GAP-001, OQ-001, TD-02).
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "MediatR notification handler adhering to <EventName>Handler convention.")]
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class StudentEnrolledEventHandler(
    ISqlConnectionFactory connectionFactory,
    ILogger<StudentEnrolledEventHandler> logger) : INotificationHandler<StudentEnrolledEvent>
{
    private const string _checkApplicantAccountExistsSql = """
        SELECT COUNT(1)
        FROM finance.student_financial_accounts
        WHERE applicant_id = @ApplicantId;
        """;

    private const string _graduateApplicantSql = """
        UPDATE finance.student_financial_accounts
        SET student_id = @StudentId,
            updated_at = SYSUTCDATETIME(),
            updated_by = 'SYSTEM_ENROLLMENT'
        WHERE applicant_id = @ApplicantId AND student_id IS NULL;

        UPDATE finance.financial_obligations
        SET student_id = @StudentId
        WHERE applicant_id = @ApplicantId AND student_id IS NULL;

        UPDATE finance.payments
        SET student_id = @StudentId
        WHERE applicant_id = @ApplicantId AND student_id IS NULL;
        """;

    private const string _checkStudentAccountExistsSql = """
        SELECT COUNT(1)
        FROM finance.student_financial_accounts
        WHERE student_id = @StudentId;
        """;

    private const string _insertAccountSql = """
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
            'SYSTEM_ENROLLMENT'
        );
        """;

    /// <inheritdoc />
    public async Task Handle(StudentEnrolledEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification.StudentId == Guid.Empty)
        {
            logger.LogWarning("Received StudentEnrolledEvent with empty StudentId; skipping account provisioning.");
            return;
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Check if an account exists for the pre-enrollment applicant (GAP-001, OQ-001, TD-02)
        if (notification.ApplicantId != Guid.Empty)
        {
            var applicantAccountCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _checkApplicantAccountExistsSql,
                new { notification.ApplicantId },
                cancellationToken: cancellationToken));

            if (applicantAccountCount > 0)
            {
                try
                {
                    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                    await connection.ExecuteAsync(new CommandDefinition(
                        _graduateApplicantSql,
                        new
                        {
                            notification.ApplicantId,
                            notification.StudentId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    await transaction.CommitAsync(cancellationToken);

                    logger.LogInformation(
                        "Graduated pre-enrollment financial account, obligations, and payments from applicant {ApplicantId} to student {StudentId}.",
                        notification.ApplicantId,
                        notification.StudentId);
                }
                catch (SqlException ex) when (ex.Number is 2601 or 2627)
                {
                    logger.LogWarning(
                        ex,
                        "Student financial account graduation for applicant {ApplicantId} to student {StudentId} was processed concurrently; completing successfully.",
                        notification.ApplicantId,
                        notification.StudentId);
                }

                return;
            }
        }

        // 2. Applicant account not found; check if account already exists for student_id (idempotent fallback)
        var studentAccountCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkStudentAccountExistsSql,
            new { notification.StudentId },
            cancellationToken: cancellationToken));

        if (studentAccountCount > 0)
        {
            logger.LogInformation(
                "Student financial account already exists for student {StudentId}. Skipping account provisioning.",
                notification.StudentId);
            return;
        }

        // 3. Account does not exist; provision a new active account for the enrolled student
        var accountNumber = $"ACC-{DateTime.UtcNow:yyyy}-{notification.StudentId.ToString()[..8].ToUpperInvariant()}";
        var accountId = Guid.NewGuid();
        var openedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertAccountSql,
                new
                {
                    Id = accountId,
                    AccountNumber = accountNumber,
                    notification.StudentId,
                    OpenedDate = openedDate
                },
                cancellationToken: cancellationToken));

            logger.LogInformation(
                "Provisioned active financial account {AccountNumber} ({AccountId}) for enrolled student {StudentId}.",
                accountNumber,
                accountId,
                notification.StudentId);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            logger.LogWarning(
                ex,
                "Student financial account for student {StudentId} was provisioned concurrently; completing successfully.",
                notification.StudentId);
        }
    }
}
