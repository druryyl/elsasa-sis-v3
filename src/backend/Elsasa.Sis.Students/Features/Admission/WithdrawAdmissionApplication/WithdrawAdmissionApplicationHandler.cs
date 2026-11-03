namespace Elsasa.Sis.Students.Features.Admission.WithdrawAdmissionApplication;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing admission application withdrawal with audit notes (ADR-004, ADR-005).
/// </summary>
public sealed class WithdrawAdmissionApplicationHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectApplicationSql = """
        SELECT
            id AS Id,
            status AS Status,
            notes AS Notes
        FROM students.admission_applications
        WHERE id = @Id;
        """;

    private const string _updateStatusSql = """
        UPDATE students.admission_applications
        SET
            status = 'withdrawn',
            notes = CASE
                WHEN notes IS NULL OR notes = '' THEN CONCAT('Withdrawal Reason: ', @WithdrawalReason)
                ELSE CONCAT(notes, CHAR(10), 'Withdrawal Reason: ', @WithdrawalReason)
            END,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        OUTPUT INSERTED.notes AS Notes, INSERTED.updated_at AS UpdatedAt
        WHERE id = @Id;
        """;

    /// <summary>
    /// Executes the lifecycle transition to withdraw an application.
    /// </summary>
    /// <param name="id">The unique identifier of the admission application.</param>
    /// <param name="request">The withdrawal request containing reason.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the updated application status response.</returns>
    public async Task<Result<WithdrawAdmissionApplicationResponse>> HandleAsync(
        Guid id,
        WithdrawAdmissionApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var application = await connection.QuerySingleOrDefaultAsync<ApplicationRow>(new CommandDefinition(
            _selectApplicationSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (application is null)
        {
            return Result.Failure<WithdrawAdmissionApplicationResponse>(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    $"Admission application with ID '{id}' was not found."));
        }

        if (string.Equals(application.Status, "decided", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<WithdrawAdmissionApplicationResponse>(
                DomainError.Conflict(
                    "Admission.AlreadyDecided",
                    "An official decision has already been recorded for this application. Decided applications cannot be withdrawn."));
        }

        if (string.Equals(application.Status, "withdrawn", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<WithdrawAdmissionApplicationResponse>(
                DomainError.Conflict(
                    "Admission.AlreadyWithdrawn",
                    "The admission application has already been withdrawn."));
        }

        var updatedBy = currentUser.Username ?? "system";

        var updatedRow = await connection.QuerySingleAsync<UpdatedResultRow>(new CommandDefinition(
            _updateStatusSql,
            new
            {
                Id = id,
                WithdrawalReason = request.WithdrawalReason,
                UpdatedBy = updatedBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new WithdrawAdmissionApplicationResponse(id, "withdrawn", updatedRow.Notes, updatedRow.UpdatedAt));
    }

    private sealed record ApplicationRow(Guid Id, string Status, string? Notes);

    private sealed record UpdatedResultRow(string? Notes, DateTimeOffset UpdatedAt);
}
