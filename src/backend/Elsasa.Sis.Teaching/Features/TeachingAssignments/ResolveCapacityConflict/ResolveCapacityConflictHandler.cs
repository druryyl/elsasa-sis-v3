namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ResolveCapacityConflict;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler resolving or waiving a capacity conflict (TD-01, P3-S07).
/// </summary>
public sealed class ResolveCapacityConflictHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static ResolveCapacityConflictHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectConflictSql = """
        SELECT
            id AS Id,
            status AS Status,
            severity AS Severity,
            teacher_profile_id AS TeacherProfileId,
            teaching_assignment_id AS TeachingAssignmentId
        FROM teaching.capacity_conflicts
        WHERE id = @Id;
        """;

    private const string _updateConflictSql = """
        UPDATE teaching.capacity_conflicts
        SET
            status = @Status,
            resolution_action = @ResolutionAction,
            resolved_by = @ResolvedBy,
            resolved_date = @ResolvedDate,
            resolution_notes = @ResolutionNotes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @ResolvedBy
        WHERE id = @Id;
        """;

    public async Task<Result<ResolveCapacityConflictResponse>> HandleAsync(
        Guid id,
        ResolveCapacityConflictRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            return Result.Failure<ResolveCapacityConflictResponse>(DomainError.Validation(
                "Teaching.InvalidConflictId",
                "Capacity conflict ID must be a non-empty GUID."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existingConflict = await connection.QuerySingleOrDefaultAsync<ConflictRow>(new CommandDefinition(
            _selectConflictSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existingConflict is null)
        {
            return Result.Failure<ResolveCapacityConflictResponse>(DomainError.NotFound(
                "Teaching.CapacityConflictNotFound",
                $"Capacity conflict with ID '{id}' was not found."));
        }

        var targetStatus = request.Status.Trim().ToLowerInvariant();
        var auditUser = currentUser.Username ?? "system";
        var resolvedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowUtc = DateTimeOffset.UtcNow;
        var resolutionNotes = string.IsNullOrWhiteSpace(request.ResolutionNotes) ? null : request.ResolutionNotes.Trim();
        var resolutionAction = !string.IsNullOrWhiteSpace(request.ResolutionAction)
            ? request.ResolutionAction.Trim()
            : (targetStatus == "waived_with_exception" ? "WaivedWithException" : "ResolvedManually");

        await connection.ExecuteAsync(new CommandDefinition(
            _updateConflictSql,
            new
            {
                Id = id,
                Status = targetStatus,
                ResolutionAction = resolutionAction,
                ResolvedBy = auditUser,
                ResolvedDate = resolvedDate,
                ResolutionNotes = resolutionNotes
            },
            cancellationToken: cancellationToken));

        return Result.Success(new ResolveCapacityConflictResponse(
            Id: id,
            Status: targetStatus,
            ResolutionAction: resolutionAction,
            ResolvedBy: auditUser,
            ResolvedDate: resolvedDate,
            ResolvedAt: nowUtc,
            ResolutionNotes: resolutionNotes));
    }

    private sealed record ConflictRow(
        Guid Id,
        string Status,
        string Severity,
        Guid TeacherProfileId,
        Guid? TeachingAssignmentId);
}
