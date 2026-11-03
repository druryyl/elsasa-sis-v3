namespace Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;

using Dapper;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing immutable admission decision recording and post-commit event publishing (ADR-003, ADR-004, ADR-005).
/// </summary>
public sealed class RecordAdmissionDecisionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IOperationsCalendarQueryService calendarService,
    IPublisher publisher)
{
    private const string _selectApplicationSql = """
        SELECT
            id AS Id,
            applicant_id AS ApplicantId,
            academic_year_id AS AcademicYearId,
            status AS Status
        FROM students.admission_applications
        WHERE id = @Id;
        """;

    private const string _checkDecisionExistsSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM students.admission_decisions WHERE application_id = @Id
        ) THEN 1 ELSE 0 END;
        """;

    private const string _insertDecisionSql = """
        INSERT INTO students.admission_decisions (
            id,
            application_id,
            decision_type,
            decision_date,
            effective_date,
            decided_by,
            reason,
            supporting_reference,
            deferred_admission_period_id,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @ApplicationId,
            @DecisionType,
            @DecisionDate,
            @EffectiveDate,
            @DecidedBy,
            @Reason,
            @SupportingReference,
            @DeferredAdmissionPeriodId,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _updateApplicationStatusSql = """
        UPDATE students.admission_applications
        SET
            status = 'decided',
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Executes the immutable admission decision recording, validates deferrals, updates application status to decided,
    /// and publishes ApplicantAcceptedEvent post-commit if accepted.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the admission application.</param>
    /// <param name="request">The decision payload.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the recorded decision response.</returns>
    public async Task<Result<RecordAdmissionDecisionResponse>> HandleAsync(
        Guid applicationId,
        RecordAdmissionDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var decisionType = request.DecisionType?.Trim().ToLowerInvariant() ?? string.Empty;

        // 1. Cross-domain operational validation for deferral periods (ADR-001, ADR-003)
        if (string.Equals(decisionType, "deferred", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.DeferredAdmissionPeriodId.HasValue)
            {
                return Result.Failure<RecordAdmissionDecisionResponse>(
                    DomainError.Validation(
                        "Admission.DeferredPeriodRequired",
                        "A target deferred admission period must be specified when decision type is 'deferred'."));
            }

            var periodResult = await calendarService.GetOperationalPeriodByIdAsync(
                request.DeferredAdmissionPeriodId.Value,
                cancellationToken);

            if (!periodResult.IsSuccess || periodResult.Value is null)
            {
                return Result.Failure<RecordAdmissionDecisionResponse>(
                    DomainError.NotFound(
                        "Admission.DeferredPeriodNotFound",
                        $"Deferred admission period with ID '{request.DeferredAdmissionPeriodId.Value}' was not found."));
            }
        }

        // 2. Query application and verify existence and decision immutability
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var application = await connection.QuerySingleOrDefaultAsync<ApplicationRow>(new CommandDefinition(
            _selectApplicationSql,
            new { Id = applicationId },
            cancellationToken: cancellationToken));

        if (application is null)
        {
            return Result.Failure<RecordAdmissionDecisionResponse>(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    $"Admission application with ID '{applicationId}' was not found."));
        }

        if (string.Equals(application.Status, "decided", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RecordAdmissionDecisionResponse>(
                DomainError.Conflict(
                    "Admission.AlreadyDecided",
                    "An official admission decision has already been recorded for this application."));
        }

        var decisionAlreadyRecorded = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkDecisionExistsSql,
            new { Id = applicationId },
            cancellationToken: cancellationToken));

        if (decisionAlreadyRecorded)
        {
            return Result.Failure<RecordAdmissionDecisionResponse>(
                DomainError.Conflict(
                    "Admission.AlreadyDecided",
                    "An official admission decision has already been recorded for this application."));
        }

        if (string.Equals(application.Status, "withdrawn", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RecordAdmissionDecisionResponse>(
                DomainError.Conflict(
                    "Admission.ApplicationWithdrawn",
                    "Cannot record an admission decision for a withdrawn application."));
        }

        var decisionId = Guid.NewGuid();
        var auditUser = currentUser.Username ?? "system";
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        var supportingReference = string.IsNullOrWhiteSpace(request.SupportingReference) ? null : request.SupportingReference.Trim();
        var deferredPeriodId = string.Equals(decisionType, "deferred", StringComparison.OrdinalIgnoreCase)
            ? request.DeferredAdmissionPeriodId
            : null;

        // 3. Atomic transaction recording decision and transitioning application to 'decided'
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            _insertDecisionSql,
            new
            {
                Id = decisionId,
                ApplicationId = applicationId,
                DecisionType = decisionType,
                DecisionDate = request.DecisionDate,
                EffectiveDate = request.EffectiveDate,
                DecidedBy = auditUser,
                Reason = reason,
                SupportingReference = supportingReference,
                DeferredAdmissionPeriodId = deferredPeriodId,
                CreatedBy = auditUser,
            },
            transaction: tx,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _updateApplicationStatusSql,
            new
            {
                Id = applicationId,
                UpdatedBy = auditUser,
            },
            transaction: tx,
            cancellationToken: cancellationToken));

        await tx.CommitAsync(cancellationToken);

        // 4. Post-commit event notification (ADR-003, TD-04)
        if (string.Equals(decisionType, "accepted", StringComparison.OrdinalIgnoreCase))
        {
            await publisher.Publish(
                new ApplicantAcceptedEvent(application.ApplicantId, application.Id, application.AcademicYearId),
                cancellationToken);
        }

        return Result.Success(new RecordAdmissionDecisionResponse(
            decisionId,
            applicationId,
            decisionType,
            request.EffectiveDate));
    }

    private sealed record ApplicationRow(Guid Id, Guid ApplicantId, Guid AcademicYearId, string Status);
}
