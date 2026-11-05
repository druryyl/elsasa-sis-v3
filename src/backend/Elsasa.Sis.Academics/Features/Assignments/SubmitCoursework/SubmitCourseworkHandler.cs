namespace Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler for student coursework submissions, resubmission tracking, and timeliness evaluation (TD-05, TD-06, TD-07, TD-08, UC-ACD-01, P5-S11).
/// </summary>
public sealed class SubmitCourseworkHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static SubmitCourseworkHandler()
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

    private const string _selectAssignmentSql = """
        SELECT
            id AS Id,
            learning_activity_id AS LearningActivityId,
            title AS Title,
            status AS Status,
            due_date AS DueDate
        FROM academics.assignments
        WHERE id = @Id;
        """;

    private const string _selectPriorSubmissionSql = """
        SELECT TOP (1) id
        FROM academics.submissions
        WHERE assignment_id = @AssignmentId
          AND student_id = @StudentId
        ORDER BY submitted_date DESC, created_at DESC;
        """;

    private const string _insertSubmissionSql = """
        INSERT INTO academics.submissions (
            id,
            assignment_id,
            student_id,
            status,
            submitted_date,
            submission_reference,
            is_resubmission,
            previous_submission_id,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @AssignmentId,
            @StudentId,
            @Status,
            @SubmittedDate,
            @SubmissionReference,
            @IsResubmission,
            @PreviousSubmissionId,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record AssignmentCheckRow(
        Guid Id,
        Guid LearningActivityId,
        string Title,
        string Status,
        DateOnly? DueDate);

    public async Task<Result<SubmitCourseworkResponse>> HandleAsync(
        Guid assignmentId,
        SubmitCourseworkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Student authorization guard (TD-09, ADR-009)
        var isPrivileged = currentUser.Roles.Any(r => _privilegedRoles.Contains(r));
        if (!isPrivileged)
        {
            if (currentUser.StudentId is null || currentUser.StudentId.Value != request.StudentId)
            {
                return Result.Failure<SubmitCourseworkResponse>(
                    DomainError.Forbidden(
                        "Academic.UnauthorizedStudentSubmission",
                        "Students are only authorized to submit coursework on their own behalf."));
            }
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 2. Verify assignment exists
        var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentCheckRow>(new CommandDefinition(
            _selectAssignmentSql,
            new { Id = assignmentId },
            cancellationToken: cancellationToken));

        if (assignment is null)
        {
            return Result.Failure<SubmitCourseworkResponse>(
                DomainError.NotFound(
                    "Academic.AssignmentNotFound",
                    $"Coursework assignment with ID '{assignmentId}' was not found."));
        }

        // 3. Verify assignment is in released status (TD-05)
        if (!string.Equals(assignment.Status, "released", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SubmitCourseworkResponse>(
                DomainError.Conflict(
                    "Academic.InvalidAssignmentStatus",
                    $"Cannot submit coursework for assignment in '{assignment.Status}' status. Submissions are only accepted for released assignments."));
        }

        // 4. Check for prior submission to maintain resubmission lineage (UC-ACD-01)
        var priorSubmissionId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
            _selectPriorSubmissionSql,
            new
            {
                AssignmentId = assignmentId,
                request.StudentId
            },
            cancellationToken: cancellationToken));

        var (isResubmission, previousSubmissionId) = CourseworkSubmissionEvaluator.DetermineLineage(priorSubmissionId);

        // 5. Determine timeliness status (TD-06)
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var status = CourseworkSubmissionEvaluator.EvaluateTimeliness(assignment.DueDate, todayUtc);

        // 6. Insert submission record
        var submissionId = Guid.NewGuid();
        var submittedDate = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        var trimmedReference = request.SubmissionReference.Trim();
        var trimmedNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertSubmissionSql,
            new
            {
                Id = submissionId,
                AssignmentId = assignmentId,
                request.StudentId,
                Status = status,
                SubmittedDate = submittedDate,
                SubmissionReference = trimmedReference,
                IsResubmission = isResubmission,
                PreviousSubmissionId = previousSubmissionId,
                Notes = trimmedNotes,
                CreatedAt = submittedDate,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        // 7. Publish post-commit domain event (ADR-003, TD-08)
        await publisher.Publish(
            new CourseworkSubmittedEvent(
                submissionId,
                assignmentId,
                request.StudentId,
                status,
                submittedDate,
                isResubmission),
            cancellationToken);

        var response = new SubmitCourseworkResponse(
            submissionId,
            assignmentId,
            request.StudentId,
            status,
            submittedDate,
            trimmedReference,
            isResubmission,
            previousSubmissionId,
            trimmedNotes,
            submittedDate,
            createdBy);

        return Result.Success(response);
    }
}
