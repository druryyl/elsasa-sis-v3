namespace Elsasa.Sis.Students.Features.Admission.StartApplicationReview;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler transitioning an admission application from 'registered' to 'under_review' (ADR-004, ADR-005).
/// </summary>
public sealed class StartApplicationReviewHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectApplicationSql = """
        SELECT
            id AS Id,
            status AS Status
        FROM students.admission_applications
        WHERE id = @Id;
        """;

    private const string _updateStatusSql = """
        UPDATE students.admission_applications
        SET
            status = 'under_review',
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        OUTPUT INSERTED.updated_at
        WHERE id = @Id;
        """;

    /// <summary>
    /// Executes the lifecycle transition to begin reviewing an application.
    /// </summary>
    /// <param name="id">The unique identifier of the admission application.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the updated application status response.</returns>
    public async Task<Result<StartApplicationReviewResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var application = await connection.QuerySingleOrDefaultAsync<ApplicationRow>(new CommandDefinition(
            _selectApplicationSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (application is null)
        {
            return Result.Failure<StartApplicationReviewResponse>(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    $"Admission application with ID '{id}' was not found."));
        }

        if (string.Equals(application.Status, "under_review", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<StartApplicationReviewResponse>(
                DomainError.Conflict(
                    "Admission.AlreadyUnderReview",
                    "The admission application is already under review."));
        }

        if (string.Equals(application.Status, "decided", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<StartApplicationReviewResponse>(
                DomainError.Conflict(
                    "Admission.AlreadyDecided",
                    "An official decision has already been recorded for this application."));
        }

        if (!string.Equals(application.Status, "registered", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<StartApplicationReviewResponse>(
                DomainError.Conflict(
                    "Admission.InvalidStatusTransition",
                    $"Cannot start review for application in '{application.Status}' status. Only applications in 'registered' status may transition to 'under_review'."));
        }

        var updatedBy = currentUser.Username ?? "system";

        var updatedAt = await connection.ExecuteScalarAsync<DateTimeOffset>(new CommandDefinition(
            _updateStatusSql,
            new
            {
                Id = id,
                UpdatedBy = updatedBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new StartApplicationReviewResponse(id, "under_review", updatedAt));
    }

    private sealed record ApplicationRow(Guid Id, string Status);
}
