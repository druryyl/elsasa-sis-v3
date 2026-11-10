namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.ChangeTeacherStanding;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing teacher standing lifecycle transitions with mandatory justification rationale (P2-S06).
/// </summary>
public sealed class ChangeTeacherStandingHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    private const string _selectTeacherSql = """
        SELECT
            id AS Id,
            teacher_number AS TeacherNumber,
            standing AS Standing
        FROM teaching.teacher_profiles
        WHERE id = @Id;
        """;

    private const string _updateStandingSql = """
        UPDATE teaching.teacher_profiles
        SET
            standing = @Standing,
            standing_reason = @StandingReason,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        OUTPUT inserted.updated_at
        WHERE id = @Id;
        """;

    public async Task<Result<ChangeTeacherStandingResponse>> HandleAsync(
        Guid id,
        ChangeTeacherStandingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<TeacherProfileSummaryRow>(new CommandDefinition(
            _selectTeacherSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<ChangeTeacherStandingResponse>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{id}' was not found."));
        }

        var normalizedStanding = request.Standing.Trim().ToLowerInvariant();
        var normalizedReason = request.StandingReason.Trim();
        var auditUser = currentUser.Username ?? "system";

        var updatedAt = await connection.ExecuteScalarAsync<DateTimeOffset>(new CommandDefinition(
            _updateStandingSql,
            new
            {
                Id = id,
                Standing = normalizedStanding,
                StandingReason = normalizedReason,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        // Publish in-memory domain event
        var domainEvent = new TeacherStandingChangedEvent(
            id,
            existing.Standing,
            normalizedStanding,
            normalizedReason,
            auditUser);

        await publisher.Publish(domainEvent, cancellationToken);

        return Result.Success(new ChangeTeacherStandingResponse(
            id,
            existing.Standing,
            normalizedStanding,
            normalizedReason,
            auditUser,
            updatedAt));
    }

    private sealed record TeacherProfileSummaryRow(Guid Id, string TeacherNumber, string Standing);
}
