namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentSubmissions;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for querying assignment submissions with dynamic on-the-fly missing projection (SCR-ACD-02, TD-06, GAP-007, OQ-005, P5-S12).
/// </summary>
public sealed class GetAssignmentSubmissionsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetAssignmentSubmissionsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkAssignmentSql = """
        SELECT
            a.id AS Id,
            a.learning_activity_id AS LearningActivityId,
            act.learning_unit_id AS LearningUnitId,
            act.section_label AS SectionLabel,
            a.due_date AS DueDate
        FROM academics.assignments a
        INNER JOIN academics.learning_activities act ON act.id = a.learning_activity_id
        WHERE a.id = @Id;
        """;

    private const string _selectRosterSubmissionsSql = """
        WITH LatestSubmissions AS (
            SELECT
                s.id AS SubmissionId,
                s.assignment_id AS AssignmentId,
                s.student_id AS StudentId,
                s.status AS Status,
                s.submitted_date AS SubmittedDate,
                s.submission_reference AS SubmissionReference,
                s.is_resubmission AS IsResubmission,
                s.notes AS Notes,
                s.created_at AS CreatedAt,
                s.created_by AS CreatedBy,
                ROW_NUMBER() OVER (
                    PARTITION BY s.student_id 
                    ORDER BY s.submitted_date DESC, s.created_at DESC
                ) AS RowNum
            FROM academics.submissions s
            WHERE s.assignment_id = @AssignmentId
        )
        SELECT
            p.student_id AS StudentId,
            std.student_number AS StudentNumber,
            std.full_name AS StudentName,
            p.section_label AS SectionLabel,
            CASE
                WHEN ls.SubmissionId IS NOT NULL THEN ls.Status
                WHEN a.due_date IS NOT NULL AND a.due_date < CAST(SYSUTCDATETIME() AS DATE) THEN 'missing'
                ELSE 'pending'
            END AS DisplayStatus,
            ls.SubmissionId AS SubmissionId,
            ls.SubmittedDate AS SubmittedDate,
            ls.SubmissionReference AS SubmissionReference,
            ISNULL(ls.IsResubmission, 0) AS IsResubmission,
            ls.Notes AS Notes,
            ls.CreatedAt AS CreatedAt,
            ls.CreatedBy AS CreatedBy
        FROM academics.academic_participations p
        INNER JOIN academics.assignments a ON a.id = @AssignmentId
        INNER JOIN academics.learning_activities act ON act.id = a.learning_activity_id
        LEFT JOIN students.students std ON std.id = p.student_id
        LEFT JOIN LatestSubmissions ls ON ls.StudentId = p.student_id AND ls.RowNum = 1
        WHERE p.learning_unit_id = act.learning_unit_id
          AND p.status = 'in_progress'
          AND (act.section_label IS NULL OR p.section_label = act.section_label)
        ORDER BY p.section_label ASC, COALESCE(std.full_name, '') ASC, p.student_id ASC;
        """;

    private sealed record AssignmentHeaderRow(
        Guid Id,
        Guid LearningActivityId,
        Guid LearningUnitId,
        string? SectionLabel,
        DateOnly? DueDate);

    /// <summary>
    /// Executes the dynamic missing projection query for all enrolled active participants of the assignment's unit/section.
    /// </summary>
    /// <param name="assignmentId">The assignment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full student roster with on-the-fly display status and submission details.</returns>
    public async Task<Result<IReadOnlyList<AssignmentSubmissionRosterItemDto>>> HandleAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentHeaderRow>(new CommandDefinition(
            _checkAssignmentSql,
            new { Id = assignmentId },
            cancellationToken: cancellationToken));

        if (assignment is null)
        {
            return Result.Failure<IReadOnlyList<AssignmentSubmissionRosterItemDto>>(
                DomainError.NotFound(
                    "Academic.AssignmentNotFound",
                    $"Assignment with ID '{assignmentId}' was not found."));
        }

        var roster = await connection.QueryAsync<AssignmentSubmissionRosterItemDto>(new CommandDefinition(
            _selectRosterSubmissionsSql,
            new { AssignmentId = assignmentId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AssignmentSubmissionRosterItemDto>>(roster.AsList());
    }
}
