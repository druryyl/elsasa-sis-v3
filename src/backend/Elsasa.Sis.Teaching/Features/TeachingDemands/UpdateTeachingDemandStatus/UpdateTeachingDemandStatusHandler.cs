namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemandStatus;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing demand lifecycle status transitions (TD-02, P2-S04).
/// </summary>
public sealed class UpdateTeachingDemandStatusHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static UpdateTeachingDemandStatusHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectDemandSql = """
        SELECT
            id AS Id,
            demand_code AS DemandCode,
            status AS Status,
            decline_reason AS DeclineReason,
            clarification_notes AS ClarificationNotes
        FROM teaching.teaching_demands
        WHERE id = @Id;
        """;

    private const string _updateStatusSql = """
        UPDATE teaching.teaching_demands
        SET
            status = @Status,
            decline_reason = @DeclineReason,
            clarification_notes = @ClarificationNotes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        OUTPUT inserted.updated_at
        WHERE id = @Id;
        """;

    public async Task<Result<UpdateTeachingDemandStatusResponse>> HandleAsync(
        Guid id,
        UpdateTeachingDemandStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify demand exists
        var existing = await connection.QuerySingleOrDefaultAsync<DemandStatusRow>(new CommandDefinition(
            _selectDemandSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateTeachingDemandStatusResponse>(DomainError.NotFound(
                "Teaching.DemandNotFound",
                $"Teaching demand with ID '{id}' was not found."));
        }

        var normalizedTarget = request.Status.Trim().ToLowerInvariant();
        var currentStatus = existing.Status.Trim().ToLowerInvariant();

        // 2. Disallow transition if already in terminal state or same state
        if (currentStatus.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateTeachingDemandStatusResponse>(DomainError.Conflict(
                "Teaching.DemandStatusUnchanged",
                $"Teaching demand with ID '{id}' is already in '{currentStatus}' status."));
        }

        if (currentStatus is "declined" or "cancelled" or "staffed")
        {
            return Result.Failure<UpdateTeachingDemandStatusResponse>(DomainError.Conflict(
                "Teaching.DemandStatusTerminal",
                $"Teaching demand with ID '{id}' is in terminal status '{currentStatus}' and cannot be transitioned to '{normalizedTarget}'."));
        }

        // 3. Prepare parameters
        var auditUser = currentUser.Username ?? "system";
        var declineReason = normalizedTarget == "declined"
            ? request.DeclineReason?.Trim()
            : existing.DeclineReason;

        var clarificationNotes = !string.IsNullOrWhiteSpace(request.ClarificationNotes)
            ? request.ClarificationNotes.Trim()
            : existing.ClarificationNotes;

        var updatedAt = await connection.ExecuteScalarAsync<DateTimeOffset>(new CommandDefinition(
            _updateStatusSql,
            new
            {
                Id = id,
                Status = normalizedTarget,
                DeclineReason = declineReason,
                ClarificationNotes = clarificationNotes,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        return Result.Success(new UpdateTeachingDemandStatusResponse(
            id,
            existing.DemandCode,
            existing.Status,
            normalizedTarget,
            declineReason,
            clarificationNotes,
            updatedAt));
    }

    private sealed record DemandStatusRow(
        Guid Id,
        string DemandCode,
        string Status,
        string? DeclineReason,
        string? ClarificationNotes);
}
