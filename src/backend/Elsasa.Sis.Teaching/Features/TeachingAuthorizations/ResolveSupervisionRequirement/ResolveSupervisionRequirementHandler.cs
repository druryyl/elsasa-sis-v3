namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.ResolveSupervisionRequirement;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler for concluding and resolving supervision requirements (TD-05, P4-S13).
/// </summary>
public sealed class ResolveSupervisionRequirementHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ResolveSupervisionRequirementHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectRequirementWithAuthSql = """
        SELECT r.id AS Id,
               r.authorization_id AS AuthorizationId,
               r.status AS Status,
               r.start_date AS StartDate,
               r.end_date AS EndDate,
               a.teacher_profile_id AS TeacherProfileId,
               a.status AS AuthorizationStatus
        FROM teaching.supervision_requirements r
        INNER JOIN teaching.teaching_authorizations a ON r.authorization_id = a.id
        WHERE r.id = @Id;
        """;

    private const string _countOtherActiveRequirementsSql = """
        SELECT COUNT(1)
        FROM teaching.supervision_requirements
        WHERE authorization_id = @AuthorizationId
          AND id != @Id
          AND status = 'active';
        """;

    private const string _updateRequirementSql = """
        UPDATE teaching.supervision_requirements
        SET status = @Status,
            resolution_notes = @ResolutionNotes,
            end_date = COALESCE(end_date, @ResolvedDate),
            updated_at = SYSUTCDATETIME(),
            updated_by = @CurrentUser
        WHERE id = @Id;
        """;

    private const string _updateAuthorizationToActiveSql = """
        UPDATE teaching.teaching_authorizations
        SET status = 'active',
            updated_at = SYSUTCDATETIME(),
            updated_by = @CurrentUser
        WHERE id = @AuthorizationId AND status = 'authorized_under_supervision';
        """;

    /// <summary>
    /// Concludes and resolves a supervision requirement.
    /// </summary>
    /// <param name="id">The unique identifier of the supervision requirement.</param>
    /// <param name="request">The resolution request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the resolution response or domain error.</returns>
    public async Task<Result<ResolveSupervisionRequirementResponse>> HandleAsync(
        Guid id,
        ResolveSupervisionRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            return Result.Failure<ResolveSupervisionRequirementResponse>(
                DomainError.Validation("Teaching.EmptyRequirementId", "Supervision requirement ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Query target requirement and parent authorization details
        var selectCmd = new CommandDefinition(
            _selectRequirementWithAuthSql,
            new { Id = id },
            cancellationToken: cancellationToken);

        var requirement = await connection.QuerySingleOrDefaultAsync<RequirementDetail>(selectCmd);
        if (requirement is null)
        {
            return Result.Failure<ResolveSupervisionRequirementResponse>(
                DomainError.NotFound("Teaching.SupervisionRequirementNotFound", "Supervision requirement not found."));
        }

        // 2. Validate current requirement is active
        if (requirement.Status != "active")
        {
            return Result.Failure<ResolveSupervisionRequirementResponse>(
                DomainError.Validation(
                    "Teaching.SupervisionRequirement.NotActive",
                    $"Cannot resolve supervision requirement with status '{requirement.Status}'. Only active requirements can be resolved."));
        }

        var normalizedStatus = request.EffectiveStatus.Trim().ToLowerInvariant();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentUserIdentity = currentUser.Username ?? "system";

        // 3. Check for any remaining active supervision requirements for this authorization
        var countCmd = new CommandDefinition(
            _countOtherActiveRequirementsSql,
            new { AuthorizationId = requirement.AuthorizationId, Id = id },
            cancellationToken: cancellationToken);

        var remainingActiveCount = await connection.ExecuteScalarAsync<int>(countCmd);

        // 4. Evaluate promotion of parent authorization to 'active'
        var shouldPromoteToActive = (remainingActiveCount == 0)
            && (requirement.AuthorizationStatus == "authorized_under_supervision")
            && (request.UpdateAuthorizationToActive ?? (normalizedStatus is "completed" or "waived"));

        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("Active SQL connection is required for database transactions.");
        }

        // 5. Persist updates in atomic transaction
        await using var transaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updateReqCmd = new CommandDefinition(
                _updateRequirementSql,
                new
                {
                    Id = id,
                    Status = normalizedStatus,
                    ResolutionNotes = request.ResolutionNotes.Trim(),
                    ResolvedDate = today,
                    CurrentUser = currentUserIdentity
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await sqlConnection.ExecuteAsync(updateReqCmd);

            if (shouldPromoteToActive)
            {
                var updateAuthCmd = new CommandDefinition(
                    _updateAuthorizationToActiveSql,
                    new
                    {
                        AuthorizationId = requirement.AuthorizationId,
                        CurrentUser = currentUserIdentity
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await sqlConnection.ExecuteAsync(updateAuthCmd);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // 6. Publish domain event if parent authorization transitioned to 'active'
        if (shouldPromoteToActive)
        {
            await publisher.Publish(new TeachingAuthorizationStatusChangedEvent(
                requirement.AuthorizationId,
                requirement.TeacherProfileId,
                requirement.AuthorizationStatus,
                "active",
                $"Supervision requirement {id} resolved with status '{normalizedStatus}'; all supervision requirements concluded.",
                currentUserIdentity), cancellationToken);
        }

        var resultingAuthStatus = shouldPromoteToActive ? "active" : requirement.AuthorizationStatus;

        var response = new ResolveSupervisionRequirementResponse(
            requirement.Id,
            requirement.AuthorizationId,
            normalizedStatus,
            request.ResolutionNotes.Trim(),
            today,
            currentUserIdentity,
            resultingAuthStatus,
            shouldPromoteToActive);

        return Result.Success(response);
    }

    private sealed record RequirementDetail(
        Guid Id,
        Guid AuthorizationId,
        string Status,
        DateOnly StartDate,
        DateOnly? EndDate,
        Guid TeacherProfileId,
        string AuthorizationStatus);
}
