namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests verifying the batch obligation assessment billing cycle engine (P4-S12, P2-S05, GAP-003, GAP-004, TD-04, ADR-003).
/// Verifies preview mode dry-run, commit mode generation with sequence-1 due schedules and MediatR domain event publication,
/// and duplicate detection on subsequent runs.
/// </summary>
public sealed class BatchAssessmentWorkflowIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record ScheduleDbRow(
        Guid Id,
        Guid ObligationId,
        int InstallmentSequence,
        DateOnly DueDate,
        long ScheduledAmount,
        string Status);

    [SkippableFact]
    public async Task BatchAssessmentWorkflow_Should_ExecutePreview_Commit_And_SkipDuplicatesOnSubsequentRun()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (ayId, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);

        // 1. Seed charge rule attached to active policy
        const string selectRuleSql = """
            SELECT TOP (1) id
            FROM finance.charge_rules
            WHERE status = 'active'
            ORDER BY created_at ASC;
            """;

        var ruleId = await ExecuteScalarAsync<Guid?>(selectRuleSql);
        if (ruleId is null || ruleId == Guid.Empty)
        {
            // Fallback: seed policy and rule if not yet seeded
            var policyId = Guid.NewGuid();
            ruleId = Guid.NewGuid();
            await ExecuteAsync("""
                INSERT INTO finance.financial_policies (
                    id, policy_code, name, category, business_purpose, applicable_context,
                    effective_date, end_date, policy_version, status, created_at, created_by
                ) VALUES (
                    @PolicyId, @PolicyCode, 'Active Batch Policy', 'tuition', 'Batch test', 'All',
                    '2026-08-01', '2027-07-31', '1.0', 'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );

                INSERT INTO finance.charge_rules (
                    id, rule_code, policy_id, name, charge_type, is_mandatory, determination_method,
                    base_amount, currency_code, recurrence_pattern, due_rule_type, due_rule_value,
                    status, created_at, created_by
                ) VALUES (
                    @RuleId, @RuleCode, @PolicyId, 'SPP Batch Test', 'tuition_fee', 1, 'fixed_amount',
                    750000, 'IDR', 'monthly', 'fixed_day_of_month', '10',
                    'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );
                """,
                new
                {
                    PolicyId = policyId,
                    PolicyCode = $"FP-BATCH-{suffix}",
                    RuleId = ruleId.Value,
                    RuleCode = $"CR-BATCH-{suffix}"
                });
        }

        // 2. Seed 2 enrolled students with active placements matching criteria
        var student1Id = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, $"{suffix}-1", "enrolled");
        await FinanceIntegrationTestHelpers.SeedPlacementAsync(connection, student1Id, campus: "Main Campus", grade: "Grade 1");

        var student2Id = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, $"{suffix}-2", "enrolled");
        await FinanceIntegrationTestHelpers.SeedPlacementAsync(connection, student2Id, campus: "Main Campus", grade: "Grade 1");

        // Clear any previous captured events
        Factory.EventCollector.Reset();

        // 3. Execute Preview Mode: returns candidate metrics, modifies zero database records
        var previewRequest = new BatchAssessObligationsRequest(
            ChargeRuleId: ruleId.Value,
            AcademicPeriodId: periodId,
            BillingCycleLabel: $"Billing Cycle {suffix}",
            TargetCampus: "Main Campus",
            TargetGrade: "Grade 1",
            Mode: "preview");

        var previewResponse = await Client.PostAsJsonAsync("/api/finance/obligations/batch-assess", previewRequest);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var previewResult = await previewResponse.Content.ReadFromJsonAsync<BatchAssessObligationsResponse>();
        previewResult.Should().NotBeNull();
        previewResult!.TotalCandidatesCount.Should().Be(2);
        previewResult.EligibleCandidatesCount.Should().Be(2);
        previewResult.DuplicateCount.Should().Be(0);
        previewResult.CreatedObligationIds.Should().BeEmpty();
        previewResult.GrossAssessedAmount.Should().BeGreaterThan(0);
        previewResult.Candidates.Should().HaveCount(2);

        // Verify database is completely untouched by preview
        const string checkObligationsCountSql = """
            SELECT COUNT(1)
            FROM finance.financial_obligations
            WHERE charge_rule_id = @RuleId AND academic_period_id = @PeriodId;
            """;

        var oblCountAfterPreview = await ExecuteScalarAsync<int>(checkObligationsCountSql, new { RuleId = ruleId.Value, PeriodId = periodId });
        oblCountAfterPreview.Should().Be(0, "preview mode must not persist any obligations");

        // 4. Execute Commit Mode: inserts obligations and sequence-1 due schedules, publishes ObligationAssessedEvent
        var commitRequest = new BatchAssessObligationsRequest(
            ChargeRuleId: ruleId.Value,
            AcademicPeriodId: periodId,
            BillingCycleLabel: $"Billing Cycle {suffix}",
            TargetCampus: "Main Campus",
            TargetGrade: "Grade 1",
            Mode: "commit");

        var commitResponse = await Client.PostAsJsonAsync("/api/finance/obligations/batch-assess", commitRequest);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var commitResult = await commitResponse.Content.ReadFromJsonAsync<BatchAssessObligationsResponse>();
        commitResult.Should().NotBeNull();
        commitResult!.TotalCandidatesCount.Should().Be(2);
        commitResult.EligibleCandidatesCount.Should().Be(2);
        commitResult.DuplicateCount.Should().Be(0);
        commitResult.CreatedObligationIds.Should().HaveCount(2);

        // Verify obligations in database
        var oblCountAfterCommit = await ExecuteScalarAsync<int>(checkObligationsCountSql, new { RuleId = ruleId.Value, PeriodId = periodId });
        oblCountAfterCommit.Should().Be(2, "commit mode must persist all eligible obligations");

        // Verify sequence-1 due schedules for created obligations
        const string selectSchedulesSql = """
            SELECT id AS Id, obligation_id AS ObligationId, installment_sequence AS InstallmentSequence,
                   due_date AS DueDate, scheduled_amount AS ScheduledAmount, status AS Status
            FROM finance.obligation_due_schedules
            WHERE obligation_id IN @ObligationIds;
            """;

        var schedules = (await QueryAsync<ScheduleDbRow>(selectSchedulesSql, new { ObligationIds = commitResult.CreatedObligationIds })).ToList();
        schedules.Should().HaveCount(2);
        schedules.Should().OnlyContain(s => s.InstallmentSequence == 1);
        schedules.Should().OnlyContain(s => s.Status == "pending");

        // Verify ObligationAssessedEvent publication
        Factory.EventCollector.ObligationAssessedEvents.Should().HaveCount(2);
        var assessedObligationIds = Factory.EventCollector.ObligationAssessedEvents.Select(e => e.ObligationId).ToList();
        assessedObligationIds.Should().BeEquivalentTo(commitResult.CreatedObligationIds);

        // 5. Re-running batch detects existing obligations and skips duplicates
        var duplicatePreviewResponse = await Client.PostAsJsonAsync("/api/finance/obligations/batch-assess", previewRequest);
        duplicatePreviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var duplicatePreviewResult = await duplicatePreviewResponse.Content.ReadFromJsonAsync<BatchAssessObligationsResponse>();
        duplicatePreviewResult.Should().NotBeNull();
        duplicatePreviewResult!.TotalCandidatesCount.Should().Be(2);
        duplicatePreviewResult.EligibleCandidatesCount.Should().Be(0);
        duplicatePreviewResult.DuplicateCount.Should().Be(2);

        var duplicateCommitResponse = await Client.PostAsJsonAsync("/api/finance/obligations/batch-assess", commitRequest);
        duplicateCommitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var duplicateCommitResult = await duplicateCommitResponse.Content.ReadFromJsonAsync<BatchAssessObligationsResponse>();
        duplicateCommitResult.Should().NotBeNull();
        duplicateCommitResult!.TotalCandidatesCount.Should().Be(2);
        duplicateCommitResult.EligibleCandidatesCount.Should().Be(0);
        duplicateCommitResult.DuplicateCount.Should().Be(2);
        duplicateCommitResult.CreatedObligationIds.Should().BeEmpty();

        // Final count remains exactly 2
        var finalOblCount = await ExecuteScalarAsync<int>(checkObligationsCountSql, new { RuleId = ruleId.Value, PeriodId = periodId });
        finalOblCount.Should().Be(2);
    }
}
