namespace Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;

using Dapper;
using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler executing single-student ad-hoc financial obligation assessment (FIN-WF-02, UC-FIN-02, GAP-003, TD-04, ADR-003, ADR-004).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class AssessAdHocObligationHandler(
    ISqlConnectionFactory connectionFactory,
    IStudentAccountService studentAccountService,
    ICurrentUser currentUser,
    IPublisher publisher,
    ILogger<AssessAdHocObligationHandler> logger)
{
    private static readonly Guid _defaultSeedPolicyId = Guid.Parse("00000000-0000-0000-0002-000000000001");

    static AssessAdHocObligationHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkStudentExistsSql = """
        SELECT 1
        FROM students.students
        WHERE id = @StudentId;
        """;

    private const string _resolveOperationalPeriodNameSql = """
        SELECT name
        FROM operations.operational_periods
        WHERE id = @AcademicPeriodId;
        """;

    private const string _resolveAcademicYearNameSql = """
        SELECT name
        FROM operations.academic_years
        WHERE id = @AcademicPeriodId;
        """;

    private const string _checkPolicyExistsSql = """
        SELECT 1
        FROM finance.financial_policies
        WHERE id = @PolicyId;
        """;

    private const string _resolveActivePolicyIdSql = """
        SELECT TOP (1) id
        FROM finance.financial_policies
        WHERE status = 'active'
        ORDER BY effective_date DESC, created_at DESC;
        """;

    private const string _insertObligationSql = """
        INSERT INTO finance.financial_obligations (
            id,
            obligation_number,
            student_account_id,
            student_id,
            charge_rule_id,
            policy_id,
            description,
            charge_type,
            is_mandatory,
            original_amount,
            currency_code,
            issue_date,
            due_date,
            academic_period_id,
            academic_period_label,
            status,
            authorized_by,
            notes,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @ObligationNumber,
            @StudentAccountId,
            @StudentId,
            NULL,
            @PolicyId,
            @Description,
            @ChargeType,
            1,
            @OriginalAmount,
            'IDR',
            @IssueDate,
            @DueDate,
            @AcademicPeriodId,
            @AcademicPeriodLabel,
            'assessed',
            @AuthorizedBy,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _insertDueScheduleSql = """
        INSERT INTO finance.obligation_due_schedules (
            id,
            obligation_id,
            installment_sequence,
            due_date,
            scheduled_amount,
            currency_code,
            status,
            notes,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @ObligationId,
            1,
            @DueDate,
            @ScheduledAmount,
            'IDR',
            'pending',
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    public async Task<Result<AssessAdHocObligationResponse>> HandleAsync(
        AssessAdHocObligationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        // 1. Verify target student exists
        var studentExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkStudentExistsSql,
            new { request.StudentId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (!studentExists)
        {
            return Result.Failure<AssessAdHocObligationResponse>(
                DomainError.NotFound(
                    "Finance.Obligation.StudentNotFound",
                    $"Student with ID '{request.StudentId}' was not found."));
        }

        var auditUser = currentUser.Username ?? "SYSTEM_OPERATIONAL";

        // 2. Resolve or auto-provision student financial account
        var account = await studentAccountService.GetOrCreateAccountAsync(
            request.StudentId,
            connection,
            transaction,
            createdBy: auditUser,
            cancellationToken: cancellationToken);

        // 3. Resolve academic period label
        var periodLabel = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            _resolveOperationalPeriodNameSql,
            new { request.AcademicPeriodId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(periodLabel))
        {
            periodLabel = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
                _resolveAcademicYearNameSql,
                new { request.AcademicPeriodId },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        if (string.IsNullOrWhiteSpace(periodLabel))
        {
            periodLabel = $"Academic Period {request.AcademicPeriodId.ToString()[..8].ToUpperInvariant()}";
        }

        // 4. Resolve governing financial policy
        Guid resolvedPolicyId;
        if (request.PolicyId.HasValue)
        {
            var policyExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkPolicyExistsSql,
                new { PolicyId = request.PolicyId.Value },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (!policyExists)
            {
                return Result.Failure<AssessAdHocObligationResponse>(
                    DomainError.NotFound(
                        "Finance.Obligation.PolicyNotFound",
                        $"Financial policy with ID '{request.PolicyId.Value}' was not found."));
            }

            resolvedPolicyId = request.PolicyId.Value;
        }
        else
        {
            var activePolicyId = await connection.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                _resolveActivePolicyIdSql,
                transaction: transaction,
                cancellationToken: cancellationToken));

            resolvedPolicyId = activePolicyId ?? _defaultSeedPolicyId;
        }

        // 5. Persist obligation and initial sequence-1 schedule
        var obligationId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var obligationNumber = $"OBL-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var cleanDescription = request.Description.Trim();
        var cleanChargeType = request.ChargeType.Trim();
        var cleanNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertObligationSql,
            new
            {
                Id = obligationId,
                ObligationNumber = obligationNumber,
                StudentAccountId = account.Id,
                StudentId = request.StudentId,
                PolicyId = resolvedPolicyId,
                Description = cleanDescription,
                ChargeType = cleanChargeType,
                OriginalAmount = request.Amount,
                IssueDate = issueDate,
                DueDate = request.DueDate,
                AcademicPeriodId = request.AcademicPeriodId,
                AcademicPeriodLabel = periodLabel,
                AuthorizedBy = auditUser,
                Notes = cleanNotes,
                CreatedAt = now,
                CreatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            _insertDueScheduleSql,
            new
            {
                Id = scheduleId,
                ObligationId = obligationId,
                DueDate = request.DueDate,
                ScheduledAmount = request.Amount,
                Notes = "Initial sequence-1 schedule for ad-hoc assessment",
                CreatedAt = now,
                CreatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 6. Commit transaction atomically
        transaction.Commit();

        logger.LogInformation(
            "Assessed ad-hoc obligation {ObligationNumber} ({ObligationId}) of {Amount} IDR for student {StudentId}.",
            obligationNumber,
            obligationId,
            request.Amount,
            request.StudentId);

        // 7. Strictly post-commit domain event publication (ADR-003)
        await publisher.Publish(
            new ObligationAssessedEvent(obligationId, request.StudentId, request.Amount, now),
            cancellationToken);

        var response = new AssessAdHocObligationResponse(
            obligationId,
            obligationNumber,
            account.Id,
            request.StudentId,
            resolvedPolicyId,
            null,
            cleanDescription,
            cleanChargeType,
            true,
            request.Amount,
            "IDR",
            issueDate,
            request.DueDate,
            request.AcademicPeriodId,
            periodLabel,
            "assessed",
            auditUser,
            cleanNotes,
            scheduleId,
            now,
            auditUser);

        return Result.Success(response);
    }
}
