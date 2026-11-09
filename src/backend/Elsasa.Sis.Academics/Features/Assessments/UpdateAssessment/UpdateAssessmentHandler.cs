namespace Elsasa.Sis.Academics.Features.Assessments.UpdateAssessment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for updating a planned assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed class UpdateAssessmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static UpdateAssessmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentSql = """
        SELECT
            id AS Id,
            learning_unit_id AS LearningUnitId,
            learning_activity_id AS LearningActivityId,
            title AS Title,
            assessment_type AS AssessmentType,
            assessment_method AS AssessmentMethod,
            max_score AS MaxScore,
            passing_score AS PassingScore,
            reassessment_policy AS ReassessmentPolicy,
            criteria AS Criteria,
            assessment_date AS AssessmentDate,
            assessor_reference AS AssessorReference,
            status AS Status,
            notes AS Notes,
            created_at AS CreatedAt,
            created_by AS CreatedBy
        FROM academics.assessments
        WHERE id = @Id;
        """;

    private const string _updateAssessmentSql = """
        UPDATE academics.assessments
        SET
            title = @Title,
            criteria = @Criteria,
            max_score = @MaxScore,
            passing_score = @PassingScore,
            reassessment_policy = @ReassessmentPolicy,
            assessment_date = @AssessmentDate,
            assessor_reference = @AssessorReference,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record AssessmentRow(
        Guid Id,
        Guid LearningUnitId,
        Guid? LearningActivityId,
        string Title,
        string AssessmentType,
        string AssessmentMethod,
        decimal? MaxScore,
        decimal? PassingScore,
        string ReassessmentPolicy,
        string? Criteria,
        DateOnly? AssessmentDate,
        string? AssessorReference,
        string Status,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy);

    public async Task<Result<UpdateAssessmentResponse>> HandleAsync(
        Guid id,
        UpdateAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify assessment exists
        var existing = await connection.QuerySingleOrDefaultAsync<AssessmentRow>(new CommandDefinition(
            _selectAssessmentSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateAssessmentResponse>(
                DomainError.NotFound(
                    "Academic.AssessmentNotFound",
                    $"Assessment with ID '{id}' was not found."));
        }

        // 2. Guard: Must be in planned status
        if (!string.Equals(existing.Status, "planned", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateAssessmentResponse>(
                DomainError.Conflict(
                    "Academic.InvalidAssessmentStatus",
                    $"Cannot update assessment in '{existing.Status}' status. Only planned assessments can be updated."));
        }

        // 3. Prepare updated values
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";
        var trimmedTitle = request.Title.Trim();
        var criteria = string.IsNullOrWhiteSpace(request.Criteria) ? null : request.Criteria.Trim();
        var assessorReference = string.IsNullOrWhiteSpace(request.AssessorReference) ? null : request.AssessorReference.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var reassessmentPolicy = string.IsNullOrWhiteSpace(request.ReassessmentPolicy)
            ? existing.ReassessmentPolicy
            : request.ReassessmentPolicy.Trim().ToLowerInvariant();

        await connection.ExecuteAsync(new CommandDefinition(
            _updateAssessmentSql,
            new
            {
                Id = id,
                Title = trimmedTitle,
                Criteria = criteria,
                request.MaxScore,
                request.PassingScore,
                ReassessmentPolicy = reassessmentPolicy,
                request.AssessmentDate,
                AssessorReference = assessorReference,
                Notes = notes,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        var response = new UpdateAssessmentResponse(
            existing.Id,
            existing.LearningUnitId,
            existing.LearningActivityId,
            trimmedTitle,
            existing.AssessmentType,
            existing.AssessmentMethod,
            request.MaxScore,
            request.PassingScore,
            reassessmentPolicy,
            criteria,
            request.AssessmentDate,
            assessorReference,
            existing.Status,
            notes,
            existing.CreatedAt,
            existing.CreatedBy,
            updatedAt,
            updatedBy);

        return Result.Success(response);
    }
}
