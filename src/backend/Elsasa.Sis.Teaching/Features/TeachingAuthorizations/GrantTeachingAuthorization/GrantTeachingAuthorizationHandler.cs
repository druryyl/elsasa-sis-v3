namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.GrantTeachingAuthorization;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler for granting formal teaching authorizations with scope definitions, evidence linking,
/// permissive guidance warnings, and conditional supervision linking (TD-05, P4-S11).
/// </summary>
public sealed class GrantTeachingAuthorizationHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static GrantTeachingAuthorizationHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectTeacherStandingSql = """
        SELECT standing
        FROM teaching.teacher_profiles
        WHERE id = @TeacherId;
        """;

    private const string _checkCapabilitySql = """
        SELECT TOP 1 1
        FROM teaching.teaching_capabilities
        WHERE id = @CapabilityId AND teacher_profile_id = @TeacherId;
        """;

    private const string _selectEvidenceStatesSql = """
        SELECT id AS Id, verification_state AS VerificationState
        FROM teaching.qualification_evidence
        WHERE teacher_profile_id = @TeacherId AND id IN @EvidenceIds;
        """;

    private const string _checkMentorTeacherSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE id = @MentorId;
        """;

    private const string _checkAuthNumberSql = """
        SELECT TOP 1 1
        FROM teaching.teaching_authorizations
        WHERE authorization_number = @AuthorizationNumber;
        """;

    private const string _countAuthNumberPrefixSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_authorizations
        WHERE authorization_number LIKE @Prefix;
        """;

    private const string _insertAuthorizationSql = """
        INSERT INTO teaching.teaching_authorizations (
            id,
            authorization_number,
            teacher_profile_id,
            authorized_capability_id,
            scope_description,
            campus_scope,
            program_scope,
            level_scope,
            delivery_mode_scope,
            status,
            unverified_credential_warning,
            effective_start_date,
            effective_end_date,
            conditions_or_restrictions,
            decision_authority,
            approved_date,
            suspension_or_revocation_reason,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @AuthorizationNumber,
            @TeacherProfileId,
            @AuthorizedCapabilityId,
            @ScopeDescription,
            @CampusScope,
            @ProgramScope,
            @LevelScope,
            @DeliveryModeScope,
            @Status,
            @UnverifiedCredentialWarning,
            @EffectiveStartDate,
            @EffectiveEndDate,
            @ConditionsOrRestrictions,
            @DecisionAuthority,
            @ApprovedDate,
            NULL,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _insertAuthorizationEvidenceSql = """
        INSERT INTO teaching.authorization_evidence (
            authorization_id,
            qualification_evidence_id,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @AuthorizationId,
            @QualificationEvidenceId,
            NULL,
            SYSUTCDATETIME(),
            @CreatedBy
        );
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
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    /// <summary>
    /// Grants a formal teaching authorization to a teacher profile.
    /// </summary>
    /// <param name="teacherId">The unique identifier of the teacher profile.</param>
    /// <param name="request">The authorization grant request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the created teaching authorization response or domain error.</returns>
    public async Task<Result<GrantTeachingAuthorizationResponse>> HandleAsync(
        Guid teacherId,
        GrantTeachingAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (teacherId == Guid.Empty)
        {
            return Result.Failure<GrantTeachingAuthorizationResponse>(
                DomainError.Validation("Teaching.EmptyTeacherId", "Teacher profile ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify teacher profile exists and standing is 'active'
        var standing = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
            _selectTeacherStandingSql,
            new { TeacherId = teacherId },
            cancellationToken: cancellationToken));

        if (standing is null)
        {
            return Result.Failure<GrantTeachingAuthorizationResponse>(DomainError.NotFound(
                "Teaching.TeacherProfileNotFound",
                $"Teacher profile with ID '{teacherId}' was not found."));
        }

        if (!string.Equals(standing, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<GrantTeachingAuthorizationResponse>(DomainError.Validation(
                "Teaching.TeacherProfileNotActive",
                $"Teaching authorization cannot be granted to a teacher profile with standing '{standing}'. The profile standing must be 'active'."));
        }

        // 2. Validate authorized capability if specified
        if (request.AuthorizedCapabilityId.HasValue)
        {
            var capabilityExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkCapabilitySql,
                new { CapabilityId = request.AuthorizedCapabilityId.Value, TeacherId = teacherId },
                cancellationToken: cancellationToken));

            if (!capabilityExists)
            {
                return Result.Failure<GrantTeachingAuthorizationResponse>(DomainError.NotFound(
                    "Teaching.CapabilityNotFound",
                    $"Teaching capability with ID '{request.AuthorizedCapabilityId.Value}' was not found for this teacher."));
            }
        }

        // 3. Inspect linked qualification evidence records and determine unverified credential warning flag
        var linkedEvidenceIds = (request.LinkedEvidenceIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var unverifiedCredentialWarning = false;

        if (linkedEvidenceIds.Count > 0)
        {
            var evidenceRows = (await connection.QueryAsync<EvidenceStateRow>(new CommandDefinition(
                _selectEvidenceStatesSql,
                new { TeacherId = teacherId, EvidenceIds = linkedEvidenceIds },
                cancellationToken: cancellationToken))).AsList();

            if (evidenceRows.Count != linkedEvidenceIds.Count)
            {
                return Result.Failure<GrantTeachingAuthorizationResponse>(DomainError.NotFound(
                    "Teaching.QualificationEvidenceNotFound",
                    "One or more linked qualification evidence records were not found for this teacher profile."));
            }

            // If ANY linked evidence has verification_state != 'verified' (e.g. 'submitted'), flag warning
            unverifiedCredentialWarning = evidenceRows.Any(e =>
                !string.Equals(e.VerificationState, "verified", StringComparison.OrdinalIgnoreCase));
        }

        // 4. Validate supervision payload if status is 'authorized_under_supervision'
        var normalizedStatus = request.ResolvedStatus;
        var isUnderSupervision = string.Equals(normalizedStatus, "authorized_under_supervision", StringComparison.OrdinalIgnoreCase);
        GrantSupervisionPayload? supervision = null;

        if (isUnderSupervision)
        {
            supervision = request.ResolvedSupervision;
            if (supervision is null)
            {
                return Result.Failure<GrantTeachingAuthorizationResponse>(DomainError.Validation(
                    "Teaching.SupervisionRequired",
                    "Supervision requirement details are mandatory when authorization status is 'authorized_under_supervision'."));
            }

            if (supervision.SupervisingTeacherId.HasValue)
            {
                var mentorExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    _checkMentorTeacherSql,
                    new { MentorId = supervision.SupervisingTeacherId.Value },
                    cancellationToken: cancellationToken));

                if (!mentorExists)
                {
                    return Result.Failure<GrantTeachingAuthorizationResponse>(DomainError.NotFound(
                        "Teaching.MentorTeacherNotFound",
                        $"Supervising mentor teacher with ID '{supervision.SupervisingTeacherId.Value}' was not found."));
                }
            }
        }

        // 5. Generate or validate unique authorization_number
        string authorizationNumber;
        if (!string.IsNullOrWhiteSpace(request.AuthorizationNumber))
        {
            authorizationNumber = request.AuthorizationNumber.Trim();
            var duplicate = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkAuthNumberSql,
                new { AuthorizationNumber = authorizationNumber },
                cancellationToken: cancellationToken));

            if (duplicate)
            {
                return Result.Failure<GrantTeachingAuthorizationResponse>(DomainError.Conflict(
                    "Teaching.DuplicateAuthorizationNumber",
                    $"A teaching authorization with number '{authorizationNumber}' already exists."));
            }
        }
        else
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"AUTH-{year}-";
            var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countAuthNumberPrefixSql,
                new { Prefix = prefix + "%" },
                cancellationToken: cancellationToken));

            var seq = count + 1;
            while (true)
            {
                var candidate = $"{prefix}{seq:D4}";
                var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    _checkAuthNumberSql,
                    new { AuthorizationNumber = candidate },
                    cancellationToken: cancellationToken));

                if (!exists)
                {
                    authorizationNumber = candidate;
                    break;
                }
                seq++;
            }
        }

        var authorizationId = Guid.NewGuid();
        var scopeDescription = request.ResolvedScopeDescription;
        var campusScope = request.ResolvedCampusScope;
        var programScope = request.ResolvedProgramScope;
        var levelScope = request.ResolvedLevelScope;
        var deliveryModeScope = string.IsNullOrWhiteSpace(request.DeliveryModeScope) ? null : request.DeliveryModeScope.Trim();
        var effectiveStartDate = request.ResolvedEffectiveStartDate;
        var effectiveEndDate = request.ResolvedEffectiveEndDate;
        var conditionsOrRestrictions = string.IsNullOrWhiteSpace(request.ConditionsOrRestrictions) ? null : request.ConditionsOrRestrictions.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var auditUser = currentUser.Username ?? "system";
        var decisionAuthority = !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teaching Authority";
        var approvedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid? supervisionRequirementId = null;

        // 6. Execute atomic multi-table write within a single database transaction
        if (connection is SqlConnection sqlConn && sqlConn.State != System.Data.ConnectionState.Open)
        {
            await sqlConn.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 6a. Insert into teaching.teaching_authorizations
            await connection.ExecuteAsync(new CommandDefinition(
                _insertAuthorizationSql,
                new
                {
                    Id = authorizationId,
                    AuthorizationNumber = authorizationNumber,
                    TeacherProfileId = teacherId,
                    AuthorizedCapabilityId = request.AuthorizedCapabilityId,
                    ScopeDescription = scopeDescription,
                    CampusScope = campusScope,
                    ProgramScope = programScope,
                    LevelScope = levelScope,
                    DeliveryModeScope = deliveryModeScope,
                    Status = normalizedStatus,
                    UnverifiedCredentialWarning = unverifiedCredentialWarning ? 1 : 0,
                    EffectiveStartDate = effectiveStartDate.ToDateTime(TimeOnly.MinValue),
                    EffectiveEndDate = effectiveEndDate?.ToDateTime(TimeOnly.MinValue),
                    ConditionsOrRestrictions = conditionsOrRestrictions,
                    DecisionAuthority = decisionAuthority,
                    ApprovedDate = approvedDate.ToDateTime(TimeOnly.MinValue),
                    Notes = notes,
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 6b. Insert linked evidence into teaching.authorization_evidence
            foreach (var evidenceId in linkedEvidenceIds)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertAuthorizationEvidenceSql,
                    new
                    {
                        AuthorizationId = authorizationId,
                        QualificationEvidenceId = evidenceId,
                        CreatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            // 6c. Insert supervision requirement if authorized under supervision
            if (isUnderSupervision && supervision is not null)
            {
                supervisionRequirementId = Guid.NewGuid();
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertSupervisionRequirementSql,
                    new
                    {
                        Id = supervisionRequirementId.Value,
                        AuthorizationId = authorizationId,
                        SupervisingTeacherId = supervision.SupervisingTeacherId,
                        SupervisionType = supervision.SupervisionType.Trim(),
                        Description = supervision.Description.Trim(),
                        StartDate = supervision.StartDate.ToDateTime(TimeOnly.MinValue),
                        EndDate = supervision.EndDate?.ToDateTime(TimeOnly.MinValue),
                        CreatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (ex.Message.Contains("UQ_teaching_authorizations_number", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<GrantTeachingAuthorizationResponse>(DomainError.Conflict(
                    "Teaching.DuplicateAuthorizationNumber",
                    $"A teaching authorization with number '{authorizationNumber}' already exists."));
            }

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // 7. Publish in-memory domain event
        var domainEvent = new TeachingAuthorizationGrantedEvent(
            authorizationId,
            teacherId,
            authorizationNumber,
            normalizedStatus,
            unverifiedCredentialWarning,
            auditUser,
            DateTimeOffset.UtcNow);

        await publisher.Publish(domainEvent, cancellationToken);

        return Result.Success(new GrantTeachingAuthorizationResponse(
            authorizationId,
            authorizationNumber,
            teacherId,
            request.AuthorizedCapabilityId,
            scopeDescription,
            campusScope,
            programScope,
            levelScope,
            deliveryModeScope,
            normalizedStatus,
            unverifiedCredentialWarning,
            effectiveStartDate,
            effectiveEndDate,
            decisionAuthority,
            approvedDate,
            linkedEvidenceIds,
            supervisionRequirementId));
    }

    private sealed record EvidenceStateRow(Guid Id, string VerificationState);
}
