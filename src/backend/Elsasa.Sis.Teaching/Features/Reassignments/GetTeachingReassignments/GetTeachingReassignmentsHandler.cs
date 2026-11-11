namespace Elsasa.Sis.Teaching.Features.Reassignments.GetTeachingReassignments;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying teaching reassignments with filters and pagination (TD-01, P4-S12).
/// </summary>
public sealed class GetTeachingReassignmentsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeachingReassignmentsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<PaginatedList<TeachingReassignmentDto>>> HandleAsync(
        Guid? originalAssignmentId = null,
        Guid? predecessorTeacherId = null,
        Guid? replacementTeacherId = null,
        Guid? newTeacherId = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (safePage - 1) * safePageSize;

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (originalAssignmentId.HasValue && originalAssignmentId.Value != Guid.Empty)
        {
            conditions.Add("tr.original_assignment_id = @OriginalAssignmentId");
            parameters.Add("OriginalAssignmentId", originalAssignmentId.Value);
        }

        if (predecessorTeacherId.HasValue && predecessorTeacherId.Value != Guid.Empty)
        {
            conditions.Add("tr.predecessor_teacher_id = @PredecessorTeacherId");
            parameters.Add("PredecessorTeacherId", predecessorTeacherId.Value);
        }

        var effectiveNewTeacherId = replacementTeacherId ?? newTeacherId;
        if (effectiveNewTeacherId.HasValue && effectiveNewTeacherId.Value != Guid.Empty)
        {
            conditions.Add("tr.new_teacher_id = @NewTeacherId");
            parameters.Add("NewTeacherId", effectiveNewTeacherId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("tr.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var countSql = $"""
            SELECT COUNT(1)
            FROM teaching.teaching_reassignments tr
            {whereClause};
            """;

        var selectSql = $"""
            SELECT
                tr.id AS Id,
                tr.reassignment_number AS ReassignmentNumber,
                tr.original_assignment_id AS OriginalAssignmentId,
                tr.predecessor_teacher_id AS PredecessorTeacherId,
                tr.new_teacher_id AS NewTeacherId,
                tr.replacement_assignment_id AS ReplacementAssignmentId,
                tr.effective_date AS EffectiveDate,
                tr.reassignment_type AS ReassignmentType,
                tr.reason AS Reason,
                tr.requires_handover AS RequiresHandover,
                tr.handover_status AS HandoverStatus,
                tr.handover_completed_date AS HandoverCompletedDate,
                tr.handover_notes AS HandoverNotes,
                tr.status AS Status,
                tr.approved_by AS ApprovedBy,
                tr.approved_date AS ApprovedDate,
                tr.created_at AS CreatedAt
            FROM teaching.teaching_reassignments tr
            {whereClause}
            ORDER BY tr.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", safePageSize);

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
            cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<TeachingReassignmentDto>(new CommandDefinition(
            selectSql,
            parameters,
            cancellationToken: cancellationToken));

        var list = PaginatedList<TeachingReassignmentDto>.Create(rows.ToList(), totalCount, safePage, safePageSize);

        return Result.Success(list);
    }
}
