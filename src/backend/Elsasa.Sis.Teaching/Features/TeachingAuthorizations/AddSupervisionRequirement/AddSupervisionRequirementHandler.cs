namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.AddSupervisionRequirement;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler for attaching a supervision requirement to a teaching authorization (TD-05, P4-S13).
/// </summary>
public sealed class AddSupervisionRequirementHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static AddSupervisionRequirementHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAuthorizationSql = """
        SELECT id AS Id,
               teacher_profile_id AS TeacherProfileId,
               status AS Status
        FROM teaching.teaching_authorizations
        WHERE id = @AuthorizationId;
        """;

    private const string _checkMentorTeacherSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE id = @MentorId;
        """;

    private const string _updateAuthorizationToSupervisedSql = """
        UPDATE teaching.teaching_authorizations
        SET status = 'authorized_under_supervision',
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @AuthorizationId AND status = 'active';
        """;

    private const string _insertSupervisionRequirementSql = """
        INSERT INTO teaching.supervision_requirements (
            id,
            authorization_id,
            supervising_teacher_id,
            supervision_type,
            description,
            start_date,
            end_date,
            status,
            resolution_notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @AuthorizationId,
            @SupervisingTeacherId,
            @SupervisionType,
            @Description,
            @StartDate,
            @EndDate,
            'active',
            NULL,
            @CreatedAt,
            @CreatedBy
        );
        """;

    /// <summary>
    /// Attaches a supervision requirement to an existing teaching authorization.
    /// </summary>
    /// <param name="authorizationId">The unique identifier of the teaching authorization.</param>
    /// <param name="request">The supervision requirement payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the created supervision requirement response or domain error.</returns>
    public async Task<Result<AddSupervisionRequirementResponse>> HandleAsync(
        Guid authorizationId,
        AddSupervisionRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (authorizationId == Guid.Empty)
        {
            return Result.Failure<AddSupervisionRequirementResponse>(
                DomainError.Validation("Teaching.EmptyAuthorizationId", "Teaching authorization ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify target authorization exists and inspect its current status
        var authCmd = new CommandDefinition(
            _selectAuthorizationSql,
            new { AuthorizationId = authorizationId },
            cancellationToken: cancellationToken);

        var authorization = await connection.QuerySingleOrDefaultAsync<AuthorizationSummary>(authCmd);
        if (authorization is null)
        {
            return Result.Failure<AddSupervisionRequirementResponse>(
                DomainError.NotFound("Teaching.AuthorizationNotFound", "Teaching authorization not found."));
        }

        // Validate that authorization allows supervision attachment
        if (authorization.Status is "revoked" or "expired")
        {
            return Result.Failure<AddSupervisionRequirementResponse>(
                DomainError.Validation(
                    "Teaching.Authorization.InvalidStatus",
                    $"Cannot attach supervision requirement to authorization with status '{authorization.Status}'."));
        }

        // 2. Validate supervising mentor teacher if specified
        if (request.SupervisingTeacherId.HasValue)
        {
            if (request.SupervisingTeacherId.Value == authorization.TeacherProfileId)
            {
                return Result.Failure<AddSupervisionRequirementResponse>(
                    DomainError.Validation(
                        "Teaching.SupervisionRequirement.SelfMentorship",
                        "A teacher cannot be designated as their own supervising mentor."));
            }

            var mentorCmd = new CommandDefinition(
                _checkMentorTeacherSql,
                new { MentorId = request.SupervisingTeacherId.Value },
                cancellationToken: cancellationToken);

            var mentorExists = await connection.ExecuteScalarAsync<int?>(mentorCmd);
            if (!mentorExists.HasValue)
            {
                return Result.Failure<AddSupervisionRequirementResponse>(
                    DomainError.NotFound("Teaching.SupervisingMentorNotFound", "Supervising mentor teacher profile not found."));
            }
        }

        // 3. Persist requirement and conditionally promote authorization status to 'authorized_under_supervision'
        var requirementId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";
        var normalizedType = request.SupervisionType.Trim().ToLowerInvariant();

        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException("Active SQL connection is required for database transactions.");
        }

        await using var transaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            // If authorization status was 'active', update it to 'authorized_under_supervision'
            if (authorization.Status == "active")
            {
                var updateAuthCmd = new CommandDefinition(
                    _updateAuthorizationToSupervisedSql,
                    new
                    {
                        AuthorizationId = authorizationId,
                        UpdatedBy = createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await sqlConnection.ExecuteAsync(updateAuthCmd);
            }

            // Insert supervision requirement
            var insertCmd = new CommandDefinition(
                _insertSupervisionRequirementSql,
                new
                {
                    Id = requirementId,
                    AuthorizationId = authorizationId,
                    request.SupervisingTeacherId,
                    SupervisionType = normalizedType,
                    Description = request.Description.Trim(),
                    request.StartDate,
                    request.EndDate,
                    CreatedAt = now,
                    CreatedBy = createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await sqlConnection.ExecuteAsync(insertCmd);

            await transaction.CommitAsync(cancellationToken);

            var response = new AddSupervisionRequirementResponse(
                requirementId,
                authorizationId,
                request.SupervisingTeacherId,
                normalizedType,
                request.Description.Trim(),
                request.StartDate,
                request.EndDate,
                "active",
                now);

            return Result.Success(response);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed record AuthorizationSummary(
        Guid Id,
        Guid TeacherProfileId,
        string Status);
}
