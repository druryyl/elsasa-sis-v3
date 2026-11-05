namespace Elsasa.Sis.Academics.Features.Assignments.RecordSubmissionException;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for recording teacher/authority coursework exceptions (TD-06, P5-S11).
/// </summary>
public sealed class RecordSubmissionExceptionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static RecordSubmissionExceptionHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentSql = """
        SELECT
            id AS Id,
            learning_activity_id AS LearningActivityId,
            title AS Title,
            status AS Status
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

    private const string _insertSubmissionExceptionSql = """
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
            NULL,
            NULL,
            @IsResubmission,
            @PreviousSubmissionId,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record AssignmentSummaryRow(
        Guid Id,
        Guid LearningActivityId,
        string Title,
        string Status);

    public async Task<Result<RecordSubmissionExceptionResponse>> HandleAsync(
        Guid assignmentId,
        RecordSubmissionExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify assignment exists
        var assignment = await connection.QuerySingleOrDefaultAsync<AssignmentSummaryRow>(new CommandDefinition(
            _selectAssignmentSql,
            new { Id = assignmentId },
            cancellationToken: cancellationToken));

        if (assignment is null)
        {
            return Result.Failure<RecordSubmissionExceptionResponse>(
                DomainError.NotFound(
                    "Academic.AssignmentNotFound",
                    $"Coursework assignment with ID '{assignmentId}' was not found."));
        }

        // 2. Check for prior submission to link resubmission lineage
        var priorSubmissionId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
            _selectPriorSubmissionSql,
            new
            {
                AssignmentId = assignmentId,
                request.StudentId
            },
            cancellationToken: cancellationToken));

        var isResubmission = priorSubmissionId.HasValue;
        var previousSubmissionId = priorSubmissionId;

        // 3. Normalize status and record exception
        var status = request.ExceptionType.Trim().ToLowerInvariant();
        var submissionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        var trimmedReason = request.Reason.Trim();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertSubmissionExceptionSql,
            new
            {
                Id = submissionId,
                AssignmentId = assignmentId,
                request.StudentId,
                Status = status,
                IsResubmission = isResubmission,
                PreviousSubmissionId = previousSubmissionId,
                Notes = trimmedReason,
                CreatedAt = createdAt,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        var response = new RecordSubmissionExceptionResponse(
            submissionId,
            assignmentId,
            request.StudentId,
            status,
            SubmittedDate: null,
            SubmissionReference: null,
            isResubmission,
            previousSubmissionId,
            trimmedReason,
            createdAt,
            createdBy);

        return Result.Success(response);
    }
}
