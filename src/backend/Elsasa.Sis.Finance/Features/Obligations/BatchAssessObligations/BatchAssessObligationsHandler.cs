namespace Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Feature handler for batch obligation assessment (preview and transactional commit) (FIN-WF-02, UC-FIN-02, SCR-FIN-01, GAP-003, GAP-004, TD-04, ADR-003, ADR-004).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class BatchAssessObligationsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IStudentAccountService studentAccountService,
    IPublisher publisher,
    ILogger<BatchAssessObligationsHandler> logger)
{
    static BatchAssessObligationsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectChargeRuleWithPolicySql = """
        SELECT
            cr.id AS Id,
            cr.rule_code AS RuleCode,
            cr.policy_id AS PolicyId,
            cr.name AS Name,
            cr.charge_type AS ChargeType,
            cr.is_mandatory AS IsMandatory,
            cr.base_amount AS BaseAmount,
            cr.currency_code AS CurrencyCode,
            cr.recurrence_pattern AS RecurrencePattern,
            cr.due_rule_type AS DueRuleType,
            cr.due_rule_value AS DueRuleValue,
            cr.applicable_grade AS ApplicableGrade,
            cr.applicable_program AS ApplicableProgram,
            cr.status AS Status,
            fp.status AS PolicyStatus
        FROM finance.charge_rules cr
        INNER JOIN finance.financial_policies fp ON fp.id = cr.policy_id
        WHERE cr.id = @ChargeRuleId;
        """;

    private const string _selectOperationalPeriodSql = """
        SELECT TOP (1)
            name AS Name,
            start_date AS StartDate,
            end_date AS EndDate
        FROM operations.operational_periods
        WHERE id = @AcademicPeriodId;
        """;

    private const string _selectCandidatesSql = """
        SELECT
            s.id AS StudentId,
            s.student_number AS StudentNumber,
            s.full_name AS StudentFullName,
            sfa.id AS AccountId,
            sfa.account_number AS AccountNumber,
            p.campus AS Campus,
            p.grade AS Grade,
            p.program AS Program,
            CASE WHEN existing_obl.id IS NOT NULL THEN 1 ELSE 0 END AS IsDuplicate
        FROM students.students s
        INNER JOIN students.administrative_placements p
            ON p.student_id = s.id AND p.is_current = 1
        LEFT JOIN finance.student_financial_accounts sfa
            ON sfa.student_id = s.id
        OUTER APPLY (
            SELECT TOP (1) fo.id
            FROM finance.financial_obligations fo
            WHERE (fo.student_account_id = sfa.id OR fo.student_id = s.id)
              AND fo.charge_rule_id = @ChargeRuleId
              AND fo.academic_period_id = @AcademicPeriodId
              AND fo.status != 'cancelled'
        ) existing_obl
        WHERE s.current_status IN ('enrolled', 'active')
          AND (@ApplicableProgram IS NULL OR p.program = @ApplicableProgram)
          AND (@ApplicableGrade IS NULL OR p.grade = @ApplicableGrade)
          AND (@TargetCampus IS NULL OR p.campus = @TargetCampus)
          AND (@TargetGrade IS NULL OR p.grade = @TargetGrade)
        ORDER BY s.student_number ASC;
        """;

    private const string _selectMaxSequenceSql = """
        SELECT ISNULL(MAX(TRY_CAST(RIGHT(obligation_number, 6) AS INT)), 0)
        FROM finance.financial_obligations WITH (UPDLOCK, HOLDLOCK)
        WHERE obligation_number LIKE @Prefix;
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
            @ChargeRuleId,
            @PolicyId,
            @Description,
            @ChargeType,
            @IsMandatory,
            @OriginalAmount,
            @CurrencyCode,
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

    private const string _insertScheduleSql = """
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
            @CurrencyCode,
            'pending',
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    public async Task<Result<BatchAssessObligationsResponse>> HandleAsync(
        BatchAssessObligationsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var isCommit = string.Equals(request.Mode?.Trim(), "commit", StringComparison.OrdinalIgnoreCase);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Rule Verification: validate existence and active status of charge rule and policy
        var rule = await connection.QuerySingleOrDefaultAsync<ChargeRuleWithPolicyRow>(
            new CommandDefinition(
                _selectChargeRuleWithPolicySql,
                new { ChargeRuleId = request.ChargeRuleId },
                cancellationToken: cancellationToken));

        if (rule is null)
        {
            return Result.Failure<BatchAssessObligationsResponse>(
                DomainError.NotFound(
                    "Finance.ChargeRule.NotFound",
                    $"Charge rule with ID '{request.ChargeRuleId}' was not found."));
        }

        if (!string.Equals(rule.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<BatchAssessObligationsResponse>(
                DomainError.Conflict(
                    "Finance.ChargeRule.Inactive",
                    $"Charge rule '{rule.RuleCode}' is not active (current status: '{rule.Status}')."));
        }

        if (!string.Equals(rule.PolicyStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<BatchAssessObligationsResponse>(
                DomainError.Conflict(
                    "Finance.FinancialPolicy.Inactive",
                    $"Governing financial policy for rule '{rule.RuleCode}' is not active (current status: '{rule.PolicyStatus}')."));
        }

        // 2. Resolve academic period details
        var period = await connection.QuerySingleOrDefaultAsync<OperationalPeriodRow>(
            new CommandDefinition(
                _selectOperationalPeriodSql,
                new { AcademicPeriodId = request.AcademicPeriodId },
                cancellationToken: cancellationToken));

        var periodReferenceDate = period?.StartDate;
        var periodLabel = !string.IsNullOrWhiteSpace(request.BillingCycleLabel)
            ? request.BillingCycleLabel.Trim()
            : period?.Name ?? "Academic Period";

        // 3. Candidate Evaluation & Duplicate Detection
        var candidateRows = (await connection.QueryAsync<CandidateStudentRow>(
            new CommandDefinition(
                _selectCandidatesSql,
                new
                {
                    ChargeRuleId = request.ChargeRuleId,
                    AcademicPeriodId = request.AcademicPeriodId,
                    ApplicableProgram = string.IsNullOrWhiteSpace(rule.ApplicableProgram)
                    || string.Equals(rule.ApplicableProgram.Trim(), "All", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : rule.ApplicableProgram.Trim(),
                    ApplicableGrade = string.IsNullOrWhiteSpace(rule.ApplicableGrade) ? null : rule.ApplicableGrade.Trim(),
                    TargetCampus = string.IsNullOrWhiteSpace(request.TargetCampus) ? null : request.TargetCampus.Trim(),
                    TargetGrade = string.IsNullOrWhiteSpace(request.TargetGrade) ? null : request.TargetGrade.Trim()
                },
                cancellationToken: cancellationToken))).ToList();

        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueDate = DueDateCalculationEngine.CalculateDueDate(
            rule.DueRuleType,
            rule.DueRuleValue,
            issueDate,
            periodReferenceDate);

        var candidateDtos = new List<BatchAssessCandidateDto>(candidateRows.Count);
        foreach (var row in candidateRows)
        {
            var isDuplicate = row.IsDuplicate == 1;
            candidateDtos.Add(new BatchAssessCandidateDto(
                StudentId: row.StudentId,
                StudentNumber: row.StudentNumber,
                StudentFullName: row.StudentFullName,
                Campus: row.Campus,
                Grade: row.Grade,
                AccountId: row.AccountId,
                AccountNumber: row.AccountNumber,
                BaseAmount: rule.BaseAmount,
                DueDate: dueDate,
                IsDuplicate: isDuplicate,
                Notes: isDuplicate ? "Duplicate: Obligation already exists for this rule and academic period." : null));
        }

        var totalCandidatesCount = candidateDtos.Count;
        var duplicateCount = candidateDtos.Count(c => c.IsDuplicate);
        var eligibleCandidates = candidateDtos.Where(c => !c.IsDuplicate).ToList();
        var eligibleCandidatesCount = eligibleCandidates.Count;
        var grossAssessedAmount = (long)eligibleCandidatesCount * rule.BaseAmount;

        // 4. Preview Mode: performs zero database writes and returns preview projection
        if (!isCommit)
        {
            return Result.Success(new BatchAssessObligationsResponse(
                TotalCandidatesCount: totalCandidatesCount,
                EligibleCandidatesCount: eligibleCandidatesCount,
                DuplicateCount: duplicateCount,
                GrossAssessedAmount: grossAssessedAmount,
                Candidates: candidateDtos,
                CreatedObligationIds: Array.Empty<Guid>()));
        }

        // 5. Commit Mode: Atomic multi-table batch inserts
        if (eligibleCandidatesCount == 0)
        {
            return Result.Success(new BatchAssessObligationsResponse(
                TotalCandidatesCount: totalCandidatesCount,
                EligibleCandidatesCount: 0,
                DuplicateCount: duplicateCount,
                GrossAssessedAmount: 0,
                Candidates: candidateDtos,
                CreatedObligationIds: Array.Empty<Guid>()));
        }

        var year = issueDate.Year;
        var prefix = $"OBL-{year}-%";
        var now = DateTimeOffset.UtcNow;
        var auditUser = currentUser.Username ?? "SYSTEM";
        var description = !string.IsNullOrWhiteSpace(rule.Name) ? rule.Name : "Fee Assessment";

        var createdObligations = new List<(Guid Id, Guid StudentId, long Amount)>(eligibleCandidatesCount);
        var createdObligationIds = new List<Guid>(eligibleCandidatesCount);

        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var currentSequence = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    _selectMaxSequenceSql,
                    new { Prefix = prefix },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            foreach (var candidate in eligibleCandidates)
            {
                var accountId = candidate.AccountId;
                if (accountId is null || accountId == Guid.Empty)
                {
                    var account = await studentAccountService.GetOrCreateAccountAsync(
                        candidate.StudentId,
                        connection,
                        tx,
                        auditUser,
                        cancellationToken);
                    accountId = account.Id;
                }

                currentSequence++;
                var obligationNumber = $"OBL-{year}-{currentSequence:D6}";
                var obligationId = Guid.NewGuid();
                var scheduleId = Guid.NewGuid();

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertObligationSql,
                    new
                    {
                        Id = obligationId,
                        ObligationNumber = obligationNumber,
                        StudentAccountId = accountId.Value,
                        StudentId = candidate.StudentId,
                        ChargeRuleId = rule.Id,
                        PolicyId = rule.PolicyId,
                        Description = description,
                        ChargeType = rule.ChargeType,
                        IsMandatory = rule.IsMandatory,
                        OriginalAmount = rule.BaseAmount,
                        CurrencyCode = rule.CurrencyCode ?? "IDR",
                        IssueDate = issueDate,
                        DueDate = dueDate,
                        AcademicPeriodId = request.AcademicPeriodId,
                        AcademicPeriodLabel = periodLabel,
                        AuthorizedBy = auditUser,
                        Notes = string.IsNullOrWhiteSpace(request.BillingCycleLabel) ? null : request.BillingCycleLabel.Trim(),
                        CreatedAt = now,
                        CreatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertScheduleSql,
                    new
                    {
                        Id = scheduleId,
                        ObligationId = obligationId,
                        DueDate = dueDate,
                        ScheduledAmount = rule.BaseAmount,
                        CurrencyCode = rule.CurrencyCode ?? "IDR",
                        Notes = $"Initial sequence-1 schedule for {obligationNumber}",
                        CreatedAt = now,
                        CreatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

                createdObligations.Add((obligationId, candidate.StudentId, rule.BaseAmount));
                createdObligationIds.Add(obligationId);
            }

            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transaction failed while committing batch obligation assessment.");
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        // 6. Strictly post-commit domain event publication (ADR-003, TD-04)
        foreach (var obl in createdObligations)
        {
            await publisher.Publish(
                new ObligationAssessedEvent(obl.Id, obl.StudentId, obl.Amount, now),
                cancellationToken);
        }

        logger.LogInformation(
            "BatchAssessObligations committed successfully: {Count} obligations created for rule {RuleCode} ({GrossAmount} IDR).",
            createdObligationIds.Count,
            rule.RuleCode,
            grossAssessedAmount);

        return Result.Success(new BatchAssessObligationsResponse(
            TotalCandidatesCount: totalCandidatesCount,
            EligibleCandidatesCount: eligibleCandidatesCount,
            DuplicateCount: duplicateCount,
            GrossAssessedAmount: grossAssessedAmount,
            Candidates: candidateDtos,
            CreatedObligationIds: createdObligationIds));
    }

    private sealed class ChargeRuleWithPolicyRow
    {
        public Guid Id { get; init; }
        public string RuleCode { get; init; } = string.Empty;
        public Guid PolicyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ChargeType { get; init; } = string.Empty;
        public bool IsMandatory { get; init; }
        public long BaseAmount { get; init; }
        public string CurrencyCode { get; init; } = "IDR";
        public string RecurrencePattern { get; init; } = string.Empty;
        public string DueRuleType { get; init; } = string.Empty;
        public string DueRuleValue { get; init; } = string.Empty;
        public string? ApplicableGrade { get; init; }
        public string? ApplicableProgram { get; init; }
        public string Status { get; init; } = string.Empty;
        public string PolicyStatus { get; init; } = string.Empty;
    }

    private sealed class OperationalPeriodRow
    {
        public string Name { get; init; } = string.Empty;
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
    }

    private sealed class CandidateStudentRow
    {
        public Guid StudentId { get; init; }
        public string StudentNumber { get; init; } = string.Empty;
        public string StudentFullName { get; init; } = string.Empty;
        public Guid? AccountId { get; init; }
        public string? AccountNumber { get; init; }
        public string Campus { get; init; } = string.Empty;
        public string Grade { get; init; } = string.Empty;
        public string? Program { get; init; }
        public int IsDuplicate { get; init; }
    }
}
