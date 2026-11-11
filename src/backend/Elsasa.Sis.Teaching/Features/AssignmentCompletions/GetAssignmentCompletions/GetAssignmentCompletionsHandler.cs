namespace Elsasa.Sis.Teaching.Features.AssignmentCompletions.GetAssignmentCompletions;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for querying assignment conclusions with optional filtering by period, teacher, and search text (TD-01, TD-04, P4-S13).
/// </summary>
public sealed class GetAssignmentCompletionsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetAssignmentCompletionsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<IReadOnlyList<AssignmentCompletionDto>>> HandleAsync(
        Guid? academicPeriodId = null,
        Guid? teacherProfileId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (academicPeriodId.HasValue && academicPeriodId.Value != Guid.Empty)
        {
            conditions.Add("ta.academic_period_id = @AcademicPeriodId");
            parameters.Add("AcademicPeriodId", academicPeriodId.Value);
        }

        if (teacherProfileId.HasValue && teacherProfileId.Value != Guid.Empty)
        {
            conditions.Add("ta.teacher_profile_id = @TeacherProfileId");
            parameters.Add("TeacherProfileId", teacherProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("(ta.assignment_number LIKE @SearchPattern OR tp.full_name LIKE @SearchPattern OR ac.completed_by LIKE @SearchPattern OR ac.academic_confirmation_source LIKE @SearchPattern OR ac.notes LIKE @SearchPattern)");
            parameters.Add("SearchPattern", $"%{search.Trim()}%");
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var sql = $"""
            SELECT
                ac.id AS Id,
                ac.teaching_assignment_id AS TeachingAssignmentId,
                ac.completion_date AS CompletionDate,
                ac.academic_confirmation_received AS AcademicConfirmationReceived,
                ac.academic_confirmation_source AS AcademicConfirmationSource,
                ac.handover_confirmed AS HandoverConfirmed,
                ac.unresolved_items_status AS UnresolvedItemsStatus,
                ac.unresolved_items_description AS UnresolvedItemsDescription,
                ac.capacity_released_date AS CapacityReleasedDate,
                ac.completed_by AS CompletedBy,
                ac.status AS Status,
                ac.notes AS Notes,
                ac.created_at AS CreatedAt
            FROM teaching.assignment_completions ac
            INNER JOIN teaching.teaching_assignments ta ON ta.id = ac.teaching_assignment_id
            LEFT JOIN teaching.teacher_profiles tp ON tp.id = ta.teacher_profile_id
            {whereClause}
            ORDER BY ac.completion_date DESC, ac.created_at DESC;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AssignmentCompletionDto>(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AssignmentCompletionDto>>(rows.ToList().AsReadOnly());
    }
}
