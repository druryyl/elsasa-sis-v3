namespace Elsasa.Sis.Academics.Features.Assessments.ConductAssessment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for marking a planned assessment as conducted (UC-ACD-05, P2-S04).
/// </summary>
public sealed class ConductAssessmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static ConductAssessmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentSql = """
        SELECT
            id AS Id,
            learning_unit_id AS LearningUnitId,
            title AS Title,
            assessment_date AS AssessmentDate,
            status AS Status,
            notes AS Notes
        FROM academics.assessments
        WHERE id = @Id;
        """;

    private const string _updateConductSql = """
        UPDATE academics.assessments
        SET
            status = 'conducted',
            assessment_date = @AssessmentDate,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record AssessmentRow(
        Guid Id,
        Guid LearningUnitId,
        string Title,
        DateOnly? AssessmentDate,
        string Status,
        string? Notes);

    public async Task<Result<ConductAssessmentResponse>> HandleAsync(
        Guid id,
        ConductAssessmentRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify assessment exists
        var existing = await connection.QuerySingleOrDefaultAsync<AssessmentRow>(new CommandDefinition(
            _selectAssessmentSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<ConductAssessmentResponse>(
                DomainError.NotFound(
                    "Academic.AssessmentNotFound",
                    $"Assessment with ID '{id}' was not found."));
        }

        // 2. Guard: Must be in planned status
        if (!string.Equals(existing.Status, "planned", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ConductAssessmentResponse>(
                DomainError.Conflict(
                    "Academic.InvalidAssessmentStatus",
                    $"Cannot mark assessment as conducted when status is '{existing.Status}'. Only planned assessments can be conducted."));
        }

        // 3. Prepare updated values
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";
        var finalAssessmentDate = request?.AssessmentDate ?? existing.AssessmentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        string? finalNotes;
        if (!string.IsNullOrWhiteSpace(request?.Notes))
        {
            finalNotes = string.IsNullOrWhiteSpace(existing.Notes)
                ? request.Notes.Trim()
                : $"{existing.Notes.Trim()}{Environment.NewLine}[Conducted]: {request.Notes.Trim()}";
        }
        else
        {
            finalNotes = existing.Notes;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            _updateConductSql,
            new
            {
                Id = id,
                AssessmentDate = finalAssessmentDate,
                Notes = finalNotes,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        var response = new ConductAssessmentResponse(
            existing.Id,
            existing.LearningUnitId,
            existing.Title,
            "conducted",
            finalAssessmentDate,
            finalNotes,
            updatedAt,
            updatedBy);

        return Result.Success(response);
    }
}
