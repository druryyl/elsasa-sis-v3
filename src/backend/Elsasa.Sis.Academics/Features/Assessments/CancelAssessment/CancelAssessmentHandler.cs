namespace Elsasa.Sis.Academics.Features.Assessments.CancelAssessment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for cancelling a planned assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed class CancelAssessmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static CancelAssessmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssessmentWithResultsSql = """
        SELECT
            a.id AS Id,
            a.learning_unit_id AS LearningUnitId,
            a.title AS Title,
            a.status AS Status,
            a.notes AS Notes,
            (SELECT COUNT(1) FROM academics.academic_results r WHERE r.assessment_id = a.id) AS ResultCount
        FROM academics.assessments a
        WHERE a.id = @Id;
        """;

    private const string _updateCancelSql = """
        UPDATE academics.assessments
        SET
            status = 'cancelled',
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record AssessmentStatusRow(
        Guid Id,
        Guid LearningUnitId,
        string Title,
        string Status,
        string? Notes,
        int ResultCount);

    public async Task<Result<CancelAssessmentResponse>> HandleAsync(
        Guid id,
        CancelAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify assessment exists
        var existing = await connection.QuerySingleOrDefaultAsync<AssessmentStatusRow>(new CommandDefinition(
            _selectAssessmentWithResultsSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<CancelAssessmentResponse>(
                DomainError.NotFound(
                    "Academic.AssessmentNotFound",
                    $"Assessment with ID '{id}' was not found."));
        }

        // 2. Guard: Cannot cancel conducted assessment or assessment with recorded results
        if (string.Equals(existing.Status, "conducted", StringComparison.OrdinalIgnoreCase) || existing.ResultCount > 0)
        {
            return Result.Failure<CancelAssessmentResponse>(
                DomainError.Conflict(
                    "Academics.Assessments.CannotCancelConductedAssessment",
                    $"Cannot cancel assessment with ID '{id}' because it is already conducted or has {existing.ResultCount} recorded academic results."));
        }

        // 3. Guard: Must be in planned status
        if (!string.Equals(existing.Status, "planned", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CancelAssessmentResponse>(
                DomainError.Conflict(
                    "Academic.InvalidAssessmentStatus",
                    $"Cannot cancel assessment in '{existing.Status}' status. Only planned assessments can be cancelled."));
        }

        // 4. Append reason to notes
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";
        var cancellationReason = request.Reason.Trim();
        var updatedNotes = string.IsNullOrWhiteSpace(existing.Notes)
            ? $"Cancellation reason: {cancellationReason}"
            : $"{existing.Notes.Trim()}{Environment.NewLine}[Cancelled]: {cancellationReason}";

        await connection.ExecuteAsync(new CommandDefinition(
            _updateCancelSql,
            new
            {
                Id = id,
                Notes = updatedNotes,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        var response = new CancelAssessmentResponse(
            existing.Id,
            existing.LearningUnitId,
            existing.Title,
            "cancelled",
            updatedNotes,
            updatedAt,
            updatedBy);

        return Result.Success(response);
    }
}
