namespace Elsasa.Sis.Academics.Features.AcademicAchievements.ConferAcademicAchievement;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using MediatR;

/// <summary>
/// Feature handler executing the formal conferral of an academic achievement or distinction (UC-ACD-21, P4-S11).
/// Persists record to <c>academics.academic_achievements</c> and publishes <see cref="AcademicAchievementConferredEvent"/> post-commit.
/// </summary>
public sealed class ConferAcademicAchievementHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher,
    IValidator<ConferAcademicAchievementCommand>? validator = null)
{
    static ConferAcademicAchievementHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkLearningUnitSql = """
        SELECT COUNT(1)
        FROM academics.learning_units
        WHERE id = @LearningUnitId;
        """;

    private const string _insertAchievementSql = """
        INSERT INTO academics.academic_achievements (
            id,
            student_id,
            learning_unit_id,
            academic_period_id,
            achievement_type,
            title,
            description,
            criteria_met,
            supporting_evidence,
            recognized_by,
            recognized_at,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @StudentId,
            @LearningUnitId,
            @AcademicPeriodId,
            @AchievementType,
            @Title,
            @Description,
            @CriteriaMet,
            @SupportingEvidence,
            @RecognizedBy,
            @RecognizedAt,
            @CreatedAt,
            @CreatedBy
        );
        """;

    /// <summary>
    /// Executes conferral of the specified academic achievement.
    /// </summary>
    public async Task<Result<AcademicAchievementDto>> HandleAsync(
        ConferAcademicAchievementCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<AcademicAchievementDto>(
                    DomainError.Validation(
                        "Academic.ValidationFailed",
                        string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        if (command.LearningUnitId.HasValue)
        {
            var unitExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkLearningUnitSql,
                new { LearningUnitId = command.LearningUnitId.Value },
                cancellationToken: cancellationToken));

            if (!unitExists)
            {
                return Result.Failure<AcademicAchievementDto>(
                    DomainError.NotFound(
                        "Academic.LearningUnitNotFound",
                        $"Learning unit with ID '{command.LearningUnitId.Value}' was not found."));
            }
        }

        var achievementId = Guid.NewGuid();
        var recognizedBy = !string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.Username
            : "Academic Authority";
        var createdBy = !string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.Username
            : "system";
        var createdAt = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(new CommandDefinition(
            _insertAchievementSql,
            new
            {
                Id = achievementId,
                command.StudentId,
                command.LearningUnitId,
                command.AcademicPeriodId,
                command.AchievementType,
                Title = command.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
                CriteriaMet = command.CriteriaMet.Trim(),
                SupportingEvidence = string.IsNullOrWhiteSpace(command.SupportingEvidence) ? null : command.SupportingEvidence.Trim(),
                RecognizedBy = recognizedBy,
                RecognizedAt = command.RecognizedAt,
                CreatedAt = createdAt,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        var domainEvent = new AcademicAchievementConferredEvent(
            achievementId,
            command.StudentId,
            command.AcademicPeriodId,
            command.AchievementType,
            command.Title.Trim(),
            createdAt);

        await publisher.Publish(domainEvent, cancellationToken);

        var dto = new AcademicAchievementDto(
            achievementId,
            command.StudentId,
            command.LearningUnitId,
            command.AcademicPeriodId,
            command.AchievementType,
            command.Title.Trim(),
            string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            command.CriteriaMet.Trim(),
            string.IsNullOrWhiteSpace(command.SupportingEvidence) ? null : command.SupportingEvidence.Trim(),
            recognizedBy,
            command.RecognizedAt);

        return Result.Success(dto);
    }
}
