namespace Elsasa.Sis.Teaching.Features.Reassignments.ConfirmReassignmentHandover;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler confirming syllabus, materials, and grading handover for a reassignment (TD-01, P4-S12).
/// </summary>
public sealed class ConfirmReassignmentHandoverHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static ConfirmReassignmentHandoverHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectReassignmentSql = """
        SELECT
            id AS Id,
            reassignment_number AS ReassignmentNumber,
            original_assignment_id AS OriginalAssignmentId,
            predecessor_teacher_id AS PredecessorTeacherId,
            new_teacher_id AS NewTeacherId,
            replacement_assignment_id AS ReplacementAssignmentId,
            effective_date AS EffectiveDate,
            reassignment_type AS ReassignmentType,
            reason AS Reason,
            requires_handover AS RequiresHandover,
            handover_status AS HandoverStatus,
            handover_completed_date AS HandoverCompletedDate,
            handover_notes AS HandoverNotes,
            status AS Status,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate,
            created_at AS CreatedAt
        FROM teaching.teaching_reassignments
        WHERE id = @Id;
        """;

    private const string _updateHandoverSql = """
        UPDATE teaching.teaching_reassignments
        SET handover_status = @HandoverStatus,
            handover_completed_date = @HandoverCompletedDate,
            handover_notes = CASE WHEN @Notes IS NOT NULL AND @Notes <> '' THEN @Notes ELSE handover_notes END,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    public async Task<Result<ConfirmReassignmentHandoverResponse>> HandleAsync(
        Guid id,
        ConfirmReassignmentHandoverRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<ReassignmentRow>(new CommandDefinition(
            _selectReassignmentSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<ConfirmReassignmentHandoverResponse>(DomainError.NotFound(
                "Teaching.ReassignmentNotFound",
                $"Reassignment with ID '{id}' was not found."));
        }

        if (string.Equals(existing.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ConfirmReassignmentHandoverResponse>(DomainError.Conflict(
                "Teaching.ReassignmentCancelled",
                $"Reassignment '{existing.ReassignmentNumber}' has been cancelled and cannot have handover confirmed."));
        }

        var targetStatus = string.IsNullOrWhiteSpace(request.HandoverStatus)
            ? "completed"
            : request.HandoverStatus.Trim().ToLowerInvariant();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var auditUser = !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teaching Coordinator";

        await connection.ExecuteAsync(new CommandDefinition(
            _updateHandoverSql,
            new
            {
                Id = id,
                HandoverStatus = targetStatus,
                HandoverCompletedDate = today,
                Notes = request.Notes?.Trim(),
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        var updatedDto = new TeachingReassignmentDto(
            Id: existing.Id,
            ReassignmentNumber: existing.ReassignmentNumber,
            OriginalAssignmentId: existing.OriginalAssignmentId,
            PredecessorTeacherId: existing.PredecessorTeacherId,
            NewTeacherId: existing.NewTeacherId,
            ReplacementAssignmentId: existing.ReplacementAssignmentId,
            EffectiveDate: existing.EffectiveDate,
            ReassignmentType: existing.ReassignmentType,
            Reason: existing.Reason,
            RequiresHandover: existing.RequiresHandover,
            HandoverStatus: targetStatus,
            HandoverCompletedDate: today,
            HandoverNotes: !string.IsNullOrWhiteSpace(request.Notes) ? request.Notes.Trim() : existing.HandoverNotes,
            Status: existing.Status,
            ApprovedBy: existing.ApprovedBy,
            ApprovedDate: existing.ApprovedDate,
            CreatedAt: existing.CreatedAt);

        return Result.Success(new ConfirmReassignmentHandoverResponse(updatedDto));
    }

    private sealed record ReassignmentRow(
        Guid Id,
        string ReassignmentNumber,
        Guid OriginalAssignmentId,
        Guid PredecessorTeacherId,
        Guid NewTeacherId,
        Guid? ReplacementAssignmentId,
        DateOnly EffectiveDate,
        string ReassignmentType,
        string Reason,
        bool RequiresHandover,
        string HandoverStatus,
        DateOnly? HandoverCompletedDate,
        string? HandoverNotes,
        string Status,
        string ApprovedBy,
        DateOnly ApprovedDate,
        DateTimeOffset CreatedAt);
}
