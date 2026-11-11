namespace Elsasa.Sis.Teaching.Features.Workload.CancelUnavailabilityWindow;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler cancelling an active teacher unavailability window (TD-04, P3-S08).
/// </summary>
public sealed class CancelUnavailabilityWindowHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static CancelUnavailabilityWindowHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectWindowSql = """
        SELECT
            id AS Id,
            teacher_profile_id AS TeacherProfileId,
            status AS Status
        FROM teaching.teacher_unavailability_windows
        WHERE id = @Id;
        """;

    private const string _cancelWindowSql = """
        UPDATE teaching.teacher_unavailability_windows
        SET status = 'cancelled',
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    public async Task<Result<CancelUnavailabilityWindowResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<CancelUnavailabilityWindowResponse>(DomainError.Validation(
                "Teaching.InvalidWindowId",
                "Unavailability window ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<WindowRecord>(new CommandDefinition(
            _selectWindowSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<CancelUnavailabilityWindowResponse>(DomainError.NotFound(
                "Teaching.UnavailabilityWindowNotFound",
                $"Unavailability window with ID '{id}' was not found."));
        }

        if (existing.Status == "cancelled")
        {
            return Result.Failure<CancelUnavailabilityWindowResponse>(DomainError.Conflict(
                "Teaching.WindowAlreadyCancelled",
                $"Unavailability window with ID '{id}' is already cancelled."));
        }

        var auditUser = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _cancelWindowSql,
            new
            {
                Id = id,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        return Result.Success(new CancelUnavailabilityWindowResponse(
            id,
            existing.TeacherProfileId,
            "cancelled",
            DateTimeOffset.UtcNow));
    }

    private sealed record WindowRecord(
        Guid Id,
        Guid TeacherProfileId,
        string Status);
}
