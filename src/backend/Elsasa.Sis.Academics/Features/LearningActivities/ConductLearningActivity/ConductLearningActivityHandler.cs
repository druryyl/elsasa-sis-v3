namespace Elsasa.Sis.Academics.Features.LearningActivities.ConductLearningActivity;

using Dapper;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler for conducting a scheduled learning activity (ADR-003, ADR-004, ADR-005, P3-S07, UC-ACD-02).
/// Updates status to 'conducted', records delivery notes, and publishes post-commit domain event.
/// </summary>
public sealed class ConductLearningActivityHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ConductLearningActivityHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActivitySql = """
        SELECT
            id AS Id,
            learning_unit_id AS LearningUnitId,
            activity_type AS ActivityType,
            title AS Title,
            section_label AS SectionLabel,
            activity_date AS ActivityDate,
            status AS Status,
            teacher_name AS TeacherName
        FROM academics.learning_activities
        WHERE id = @Id;
        """;

    private const string _updateActivitySql = """
        UPDATE academics.learning_activities
        SET
            status = 'conducted',
            delivery_notes = @DeliveryNotes,
            teacher_name = COALESCE(@ActualTeacherName, teacher_name),
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record ActivityRow(
        Guid Id,
        Guid LearningUnitId,
        string ActivityType,
        string Title,
        string? SectionLabel,
        DateOnly ActivityDate,
        string Status,
        string? TeacherName);

    public async Task<Result<ConductLearningActivityResponse>> HandleAsync(
        Guid id,
        ConductLearningActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify activity exists
        var activity = await connection.QuerySingleOrDefaultAsync<ActivityRow>(new CommandDefinition(
            _selectActivitySql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (activity is null)
        {
            return Result.Failure<ConductLearningActivityResponse>(
                DomainError.NotFound(
                    "Academic.LearningActivityNotFound",
                    $"Learning activity with ID '{id}' was not found."));
        }

        // 2. Verify planned status
        if (!string.Equals(activity.Status, "planned", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ConductLearningActivityResponse>(
                DomainError.Conflict(
                    "Academic.InvalidActivityStatus",
                    $"Cannot conduct learning activity in '{activity.Status}' status. Only planned activities can be conducted."));
        }

        // 3. Update activity
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";
        var finalTeacherName = !string.IsNullOrWhiteSpace(request.ActualTeacherName)
            ? request.ActualTeacherName.Trim()
            : activity.TeacherName;

        await connection.ExecuteAsync(new CommandDefinition(
            _updateActivitySql,
            new
            {
                Id = id,
                request.DeliveryNotes,
                ActualTeacherName = string.IsNullOrWhiteSpace(request.ActualTeacherName) ? null : request.ActualTeacherName.Trim(),
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            },
            cancellationToken: cancellationToken));

        // 4. Publish post-commit domain event via MediatR IPublisher (ADR-003, TD-08)
        await publisher.Publish(
            new LearningActivityConductedEvent(
                activity.Id,
                activity.LearningUnitId,
                activity.ActivityDate,
                activity.ActivityType,
                activity.SectionLabel),
            cancellationToken);

        var response = new ConductLearningActivityResponse(
            activity.Id,
            activity.LearningUnitId,
            activity.ActivityType,
            activity.Title,
            activity.SectionLabel,
            activity.ActivityDate,
            "conducted",
            request.DeliveryNotes,
            finalTeacherName,
            updatedAt,
            updatedBy);

        return Result.Success(response);
    }
}
