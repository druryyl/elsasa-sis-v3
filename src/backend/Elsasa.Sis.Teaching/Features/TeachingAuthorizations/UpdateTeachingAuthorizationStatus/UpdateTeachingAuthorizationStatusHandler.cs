namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.UpdateTeachingAuthorizationStatus;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing teaching authorization lifecycle status transitions with mandatory justification rationale (TD-05, P4-S12).
/// </summary>
public sealed class UpdateTeachingAuthorizationStatusHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static UpdateTeachingAuthorizationStatusHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAuthorizationSql = """
        SELECT
            id AS Id,
            teacher_profile_id AS TeacherProfileId,
            authorization_number AS AuthorizationNumber,
            status AS Status
        FROM teaching.teaching_authorizations
        WHERE id = @Id;
        """;

    private const string _updateStatusSql = """
        UPDATE teaching.teaching_authorizations
        SET
            status = @Status,
            suspension_or_revocation_reason = @StatusReason,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        OUTPUT inserted.updated_at
        WHERE id = @Id;
        """;

    public async Task<Result<UpdateTeachingAuthorizationStatusResponse>> HandleAsync(
        Guid id,
        UpdateTeachingAuthorizationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<AuthorizationSummaryRow>(new CommandDefinition(
            _selectAuthorizationSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateTeachingAuthorizationStatusResponse>(DomainError.NotFound(
                "Teaching.AuthorizationNotFound",
                $"Teaching authorization with ID '{id}' was not found."));
        }

        var normalizedStatus = request.ResolvedStatus;
        var normalizedReason = request.ResolvedStatusReason;
        var auditUser = currentUser.Username ?? "system";

        var updatedAt = await connection.ExecuteScalarAsync<DateTimeOffset>(new CommandDefinition(
            _updateStatusSql,
            new
            {
                Id = id,
                Status = normalizedStatus,
                StatusReason = normalizedReason,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        // Publish in-memory domain event
        var domainEvent = new TeachingAuthorizationStatusChangedEvent(
            id,
            existing.TeacherProfileId,
            existing.Status,
            normalizedStatus,
            normalizedReason,
            auditUser);

        await publisher.Publish(domainEvent, cancellationToken);

        return Result.Success(new UpdateTeachingAuthorizationStatusResponse(
            id,
            existing.TeacherProfileId,
            existing.AuthorizationNumber,
            existing.Status,
            normalizedStatus,
            normalizedReason,
            auditUser,
            updatedAt));
    }

    private sealed record AuthorizationSummaryRow(
        Guid Id,
        Guid TeacherProfileId,
        string AuthorizationNumber,
        string Status);
}
