namespace Elsasa.Sis.Academics.Features.Assessments.CreateAssessment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for creating planned assessments (UC-ACD-05, ACD-WF-07, P2-S04).
/// </summary>
public sealed class CreateAssessmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static CreateAssessmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectUnitSql = """
        SELECT
            id AS Id,
            title AS Title
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _selectActivitySql = """
        SELECT
            id AS Id,
            learning_unit_id AS LearningUnitId,
            status AS Status
        FROM academics.learning_activities
        WHERE id = @LearningActivityId;
        """;

    private const string _insertAssessmentSql = """
        INSERT INTO academics.assessments (
            id,
            learning_unit_id,
            learning_activity_id,
            title,
            assessment_type,
            assessment_method,
            max_score,
            passing_score,
            reassessment_policy,
            criteria,
            assessment_date,
            assessor_reference,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @LearningUnitId,
            @LearningActivityId,
            @Title,
            @AssessmentType,
            @AssessmentMethod,
            @MaxScore,
            @PassingScore,
            @ReassessmentPolicy,
            @Criteria,
            @AssessmentDate,
            @AssessorReference,
            'planned',
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record UnitRow(Guid Id, string Title);
    private sealed record ActivityRow(Guid Id, Guid LearningUnitId, string Status);

    public async Task<Result<CreateAssessmentResponse>> HandleAsync(
        CreateAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify parent learning unit exists
        var unit = await connection.QuerySingleOrDefaultAsync<UnitRow>(new CommandDefinition(
            _selectUnitSql,
            new { request.LearningUnitId },
            cancellationToken: cancellationToken));

        if (unit is null)
        {
            return Result.Failure<CreateAssessmentResponse>(
                DomainError.NotFound(
                    "Academic.LearningUnitNotFound",
                    $"Learning unit with ID '{request.LearningUnitId}' was not found."));
        }

        // 2. If activity ID is provided, verify it exists and belongs to the same learning unit
        if (request.LearningActivityId.HasValue && request.LearningActivityId.Value != Guid.Empty)
        {
            var activity = await connection.QuerySingleOrDefaultAsync<ActivityRow>(new CommandDefinition(
                _selectActivitySql,
                new { LearningActivityId = request.LearningActivityId.Value },
                cancellationToken: cancellationToken));

            if (activity is null)
            {
                return Result.Failure<CreateAssessmentResponse>(
                    DomainError.NotFound(
                        "Academic.LearningActivityNotFound",
                        $"Learning activity with ID '{request.LearningActivityId.Value}' was not found."));
            }

            if (activity.LearningUnitId != request.LearningUnitId)
            {
                return Result.Failure<CreateAssessmentResponse>(
                    DomainError.Conflict(
                        "Academic.LearningActivityMismatch",
                        $"Learning activity with ID '{request.LearningActivityId.Value}' belongs to unit '{activity.LearningUnitId}', not '{request.LearningUnitId}'."));
            }
        }

        // 3. Insert assessment
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        var reassessmentPolicy = string.IsNullOrWhiteSpace(request.ReassessmentPolicy)
            ? "highest_score"
            : request.ReassessmentPolicy.Trim().ToLowerInvariant();

        var normalizedType = request.AssessmentType.Trim().ToLowerInvariant();
        var normalizedMethod = request.AssessmentMethod.Trim().ToLowerInvariant();
        var trimmedTitle = request.Title.Trim();
        var criteria = string.IsNullOrWhiteSpace(request.Criteria) ? null : request.Criteria.Trim();
        var assessorReference = string.IsNullOrWhiteSpace(request.AssessorReference) ? null : request.AssessorReference.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAssessmentSql,
            new
            {
                Id = id,
                request.LearningUnitId,
                request.LearningActivityId,
                Title = trimmedTitle,
                AssessmentType = normalizedType,
                AssessmentMethod = normalizedMethod,
                request.MaxScore,
                request.PassingScore,
                ReassessmentPolicy = reassessmentPolicy,
                Criteria = criteria,
                request.AssessmentDate,
                AssessorReference = assessorReference,
                Notes = notes,
                CreatedAt = createdAt,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        var response = new CreateAssessmentResponse(
            id,
            request.LearningUnitId,
            request.LearningActivityId,
            trimmedTitle,
            normalizedType,
            normalizedMethod,
            request.MaxScore,
            request.PassingScore,
            reassessmentPolicy,
            criteria,
            request.AssessmentDate,
            assessorReference,
            "planned",
            notes,
            createdAt,
            createdBy);

        return Result.Success(response);
    }
}
