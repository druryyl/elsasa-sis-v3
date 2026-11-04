namespace Elsasa.Sis.Finance.Features.Accounts.Events;

using Dapper;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Asynchronously handles <see cref="StudentEnrolledEvent"/> to provision an active
/// student financial account in <c>finance.student_financial_accounts</c> upon student enrollment (ADR-003, GAP-002, TD-03).
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "MediatR notification handler adhering to <EventName>Handler convention.")]
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class StudentEnrolledEventHandler(
    ISqlConnectionFactory connectionFactory,
    ILogger<StudentEnrolledEventHandler> logger) : INotificationHandler<StudentEnrolledEvent>
{
    private const string _checkAccountExistsSql = """
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

        // Handler performs an idempotent check: if finance.student_financial_accounts already contains a row for StudentId, no-op and complete successfully.
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkAccountExistsSql,
            new { notification.StudentId },
            cancellationToken: cancellationToken));

        if (count > 0)
        {
            logger.LogInformation(
                "Student financial account already exists for student {StudentId}. Skipping account provisioning.",
                notification.StudentId);
            return;
        }

        // Handler generates deterministic account number format: ACC-{DateTime.UtcNow:yyyy}-{StudentId.ToString()[..8].ToUpperInvariant()}.
        var accountNumber = $"ACC-{DateTime.UtcNow:yyyy}-{notification.StudentId.ToString()[..8].ToUpperInvariant()}";
        var accountId = Guid.NewGuid();
        var openedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Handler inserts an active account record with id = Guid.NewGuid(), student_id = notification.StudentId, currency_code = 'IDR', account_status = 'active', opened_date = DateOnly.FromDateTime(DateTime.UtcNow), created_by = 'SYSTEM_ENROLLMENT'.
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
