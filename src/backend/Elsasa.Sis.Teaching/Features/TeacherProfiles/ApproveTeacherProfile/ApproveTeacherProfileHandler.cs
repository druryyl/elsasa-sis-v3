namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.ApproveTeacherProfile;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing Teaching Authority formal candidate approval,
/// transitioning teacher profile standing from 'pending_approval' to 'active' (P2-S06).
/// </summary>
public sealed class ApproveTeacherProfileHandler(
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

    private const string _approveTeacherSql = """
        UPDATE teaching.teacher_profiles
        SET
            standing = 'active',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            updated_at = SYSUTCDATETIME(),
            updated_by = @ApprovedBy
        WHERE id = @Id AND standing = 'pending_approval';
        """;

    public async Task<Result<ApproveTeacherProfileResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<TeacherProfileSummaryRow>(new CommandDefinition(
            _selectTeacherSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<ApproveTeacherProfileResponse>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{id}' was not found."));
        }

        if (!string.Equals(existing.Standing, "pending_approval", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApproveTeacherProfileResponse>(DomainError.Conflict(
                "Teaching.InvalidStandingTransition",
                $"Cannot approve teacher profile with ID '{id}' because current standing is '{existing.Standing}'. Only profiles in 'pending_approval' standing can be approved."));
        }

        var approvedBy = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            _approveTeacherSql,
            new
            {
                Id = id,
                ApprovedBy = approvedBy,
                ApprovedDate = today.ToDateTime(TimeOnly.MinValue)
            },
            cancellationToken: cancellationToken));

        if (rowsAffected == 0)
        {
            return Result.Failure<ApproveTeacherProfileResponse>(DomainError.Conflict(
                "Teaching.InvalidStandingTransition",
                $"Teacher profile with ID '{id}' could not be approved because its standing is no longer 'pending_approval'."));
        }

        // Publish in-memory domain event
        var domainEvent = new TeacherProfileApprovedEvent(
            id,
            existing.TeacherNumber,
            approvedBy,
            today);

        await publisher.Publish(domainEvent, cancellationToken);

        return Result.Success(new ApproveTeacherProfileResponse(
            id,
            existing.TeacherNumber,
            "active",
            approvedBy,
            today));
    }

    private sealed record TeacherProfileSummaryRow(Guid Id, string TeacherNumber, string Standing);
}
