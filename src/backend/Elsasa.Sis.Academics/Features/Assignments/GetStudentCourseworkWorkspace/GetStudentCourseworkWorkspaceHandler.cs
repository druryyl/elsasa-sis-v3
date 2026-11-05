namespace Elsasa.Sis.Academics.Features.Assignments.GetStudentCourseworkWorkspace;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for student self-service coursework workspace with dynamic deadline countdown and missing projections (SCR-SELF-03, TD-06, GAP-007, OQ-005, P5-S12).
/// </summary>
public sealed class GetStudentCourseworkWorkspaceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static GetStudentCourseworkWorkspaceHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private static readonly HashSet<string> _privilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Teacher",
        "AcademicCoordinator",
        "Academic Coordinator",
        "AcademicAuthority",
        "Academic Authority",
        "Admin",
        "Administrator"
    };

    private static readonly HashSet<string> _validFilters = new(StringComparer.OrdinalIgnoreCase)
    {
        "all",
        "upcoming",
        "due_soon",
        "submitted",
        "missing",
        "pending",
        "late",
        "excused",
        "exempted"
    };

    private const string _selectCourseworkTasksSql = """
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
                ROW_NUMBER() OVER (
                    PARTITION BY s.assignment_id 
                    ORDER BY s.submitted_date DESC, s.created_at DESC
                ) AS RowNum
            FROM academics.submissions s
            WHERE s.student_id = @StudentId
        ),
        TaskProjections AS (
            SELECT
                a.id AS AssignmentId,
                a.title AS Title,
                a.description AS Description,
                a.release_date AS ReleaseDate,
                a.due_date AS DueDate,
                a.status AS AssignmentStatus,
                act.id AS LearningActivityId,
                act.title AS LearningActivityTitle,
                u.id AS LearningUnitId,
                u.code AS LearningUnitCode,
                u.name AS LearningUnitName,
                p.section_label AS SectionLabel,
                CASE
                    WHEN ls.SubmissionId IS NOT NULL THEN ls.Status
                    WHEN a.due_date IS NOT NULL AND a.due_date < CAST(SYSUTCDATETIME() AS DATE) THEN 'missing'
                    ELSE 'pending'
                END AS DisplayStatus,
                CASE
                    WHEN a.due_date IS NOT NULL THEN DATEDIFF(day, CAST(SYSUTCDATETIME() AS DATE), a.due_date)
                    ELSE NULL
                END AS CountdownDays,
                ls.SubmissionId AS SubmissionId,
                ls.SubmittedDate AS SubmittedDate,
                ls.SubmissionReference AS SubmissionReference,
                ISNULL(ls.IsResubmission, 0) AS IsResubmission,
                ls.Notes AS SubmissionNotes,
                ROW_NUMBER() OVER (
                    PARTITION BY a.id 
                    ORDER BY p.start_date DESC, p.created_at DESC
                ) AS AssignmentRowNum
            FROM academics.academic_participations p
            INNER JOIN academics.learning_units u ON u.id = p.learning_unit_id
            INNER JOIN academics.learning_activities act ON act.learning_unit_id = p.learning_unit_id
                AND (act.section_label IS NULL OR p.section_label IS NULL OR act.section_label = p.section_label)
                AND act.status <> 'cancelled'
            INNER JOIN academics.assignments a ON a.learning_activity_id = act.id
                AND a.status IN ('released', 'closed')
            LEFT JOIN LatestSubmissions ls ON ls.AssignmentId = a.id AND ls.RowNum = 1
            WHERE p.student_id = @StudentId
              AND p.status = 'in_progress'
        )
        SELECT
            AssignmentId,
            Title,
            Description,
            ReleaseDate,
            DueDate,
            AssignmentStatus,
            LearningActivityId,
            LearningActivityTitle,
            LearningUnitId,
            LearningUnitCode,
            LearningUnitName,
            SectionLabel,
            DisplayStatus,
            CountdownDays,
            SubmissionId,
            SubmittedDate,
            SubmissionReference,
            IsResubmission,
            SubmissionNotes
        FROM TaskProjections
        WHERE AssignmentRowNum = 1
          AND (
              (@Filter = 'all')
              OR (@Filter = 'submitted' AND DisplayStatus IN ('submitted', 'late', 'excused', 'exempted'))
              OR (@Filter = 'missing' AND DisplayStatus = 'missing')
              OR (@Filter = 'due_soon' AND DisplayStatus = 'pending' AND CountdownDays IS NOT NULL AND CountdownDays >= 0 AND CountdownDays <= 7)
              OR (@Filter = 'upcoming' AND DisplayStatus = 'pending' AND (CountdownDays IS NULL OR CountdownDays >= 0))
              OR (@Filter = 'pending' AND DisplayStatus = 'pending')
              OR (@Filter = 'late' AND DisplayStatus = 'late')
              OR (@Filter = 'excused' AND DisplayStatus = 'excused')
              OR (@Filter = 'exempted' AND DisplayStatus = 'exempted')
          )
        ORDER BY
            CASE 
                WHEN DisplayStatus = 'pending' THEN 0 
                WHEN DisplayStatus = 'missing' THEN 1 
                ELSE 2 
            END ASC,
            CASE WHEN DueDate IS NULL THEN 1 ELSE 0 END ASC,
            DueDate ASC,
            ReleaseDate DESC;
        """;

    /// <summary>
    /// Executes coursework workspace task query across all units where student has active participation.
    /// </summary>
    /// <param name="statusFilter">Optional filter ('all', 'upcoming', 'due_soon', 'submitted', 'missing').</param>
    /// <param name="studentIdOverride">Optional student ID override for privileged roles.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of coursework tasks matching criteria.</returns>
    public async Task<Result<IReadOnlyList<StudentCourseworkTaskDto>>> HandleAsync(
        string? statusFilter = null,
        Guid? studentIdOverride = null,
        CancellationToken cancellationToken = default)
    {
        var isPrivileged = currentUser.Roles.Any(r => _privilegedRoles.Contains(r));

        Guid effectiveStudentId;
        if (currentUser.StudentId.HasValue)
        {
            if (studentIdOverride.HasValue && studentIdOverride.Value != currentUser.StudentId.Value && !isPrivileged)
            {
                return Result.Failure<IReadOnlyList<StudentCourseworkTaskDto>>(
                    DomainError.Forbidden(
                        "Academic.UnauthorizedStudentWorkspace",
                        "Students are only authorized to access their own coursework workspace."));
            }

            effectiveStudentId = currentUser.StudentId.Value;
        }
        else if (isPrivileged)
        {
            if (!studentIdOverride.HasValue)
            {
                return Result.Failure<IReadOnlyList<StudentCourseworkTaskDto>>(
                    DomainError.Validation(
                        "Academic.StudentIdRequired",
                        "Student ID must be specified when querying coursework workspace as a teacher or administrator."));
            }

            effectiveStudentId = studentIdOverride.Value;
        }
        else
        {
            return Result.Failure<IReadOnlyList<StudentCourseworkTaskDto>>(
                DomainError.Forbidden(
                    "Academic.UnauthorizedStudentWorkspace",
                    "The current user is not associated with an enrolled student."));
        }

        var normalizedFilter = string.IsNullOrWhiteSpace(statusFilter) ? "all" : statusFilter.Trim().ToLowerInvariant();
        if (!_validFilters.Contains(normalizedFilter))
        {
            return Result.Failure<IReadOnlyList<StudentCourseworkTaskDto>>(
                DomainError.Validation(
                    "Academic.InvalidStatusFilter",
                    $"Invalid status filter '{statusFilter}'. Allowed values: all, upcoming, due_soon, submitted, missing."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var tasks = await connection.QueryAsync<StudentCourseworkTaskDto>(new CommandDefinition(
            _selectCourseworkTasksSql,
            new
            {
                StudentId = effectiveStudentId,
                Filter = normalizedFilter
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<StudentCourseworkTaskDto>>(tasks.AsList());
    }
}
