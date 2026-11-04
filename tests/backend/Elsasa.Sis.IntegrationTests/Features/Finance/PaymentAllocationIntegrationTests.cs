namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using Dapper;
using Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;
using Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;
using Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration test suite verifying Smart Allocation suggestions, explicit allocation execution,
/// over-allocation invariants, and append-only reallocation lineage (P4-S12, OQ-003, TD-05).
/// </summary>
public sealed class PaymentAllocationIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record ObligationStatusDbRow(Guid Id, string Status, long OriginalAmount);
    private sealed record AllocationDbRow(
        Guid Id,
        string AllocationNumber,
        Guid ObligationId,
        Guid PaymentId,
        long AllocatedAmount,
        string Status,
        Guid? SupersededById,
        string? ReversalReason);

    [SkippableFact]
    public async Task SuggestAllocations_Should_AccuratelyPredictDistribution_PrioritizingMandatoryEnrollment_ThenOldestTuition()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var (_, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);

        // 1. Mandatory tuition due in 30 days (2,000,000 IDR)
        var oblTuition = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection,
            accountId: accountId,
            studentId: studentId,
            applicantId: null,
            policyId: policyId,
            chargeType: "tuition",
            description: "SPP Semester 1",
            amount: 2_000_000,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            academicPeriodId: periodId,
            isMandatory: true,
            status: "assessed");

        // 2. Mandatory enrollment fee due in 60 days (1,500,000 IDR) - should be prioritized FIRST despite later due date!
        var oblEnrollment = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection,
            accountId: accountId,
            studentId: studentId,
            applicantId: null,
            policyId: policyId,
            chargeType: "enrollment_fee",
            description: "Uang Pangkal Masuk",
            amount: 1_500_000,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            academicPeriodId: periodId,
            isMandatory: true,
            status: "assessed");

        // 3. Non-mandatory uniform fee due in 5 days (500,000 IDR) - lowest priority
        var oblUniform = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection,
            accountId: accountId,
            studentId: studentId,
            applicantId: null,
            policyId: policyId,
            chargeType: "uniform_fee",
            description: "Seragam Olahraga",
            amount: 500_000,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            academicPeriodId: periodId,
            isMandatory: false,
            status: "assessed");

        // Act: Request auto-suggest for payment amount of 2,500,000 IDR
        var response = await Client.GetAsync($"/api/finance/allocations/suggest?studentId={studentId}&paymentAmount=2500000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SuggestAllocationsResponse>();
        result.Should().NotBeNull();
        result!.PaymentAmount.Should().Be(2_500_000);
        result.TotalSuggestedAllocated.Should().Be(2_500_000);
        result.RemainingUnallocatedAmount.Should().Be(0);
        result.Suggestions.Should().HaveCount(2);

        // Priority 1: Mandatory enrollment fee gets 100% of its amount (1,500,000)
        result.Suggestions[0].ObligationId.Should().Be(oblEnrollment);
        result.Suggestions[0].SuggestedAmount.Should().Be(1_500_000);
        result.Suggestions[0].ChargeType.Should().Be("enrollment_fee");

        // Priority 2: Oldest mandatory tuition gets remaining 1,000,000 (out of 2,000,000)
        result.Suggestions[1].ObligationId.Should().Be(oblTuition);
        result.Suggestions[1].SuggestedAmount.Should().Be(1_000_000);
        result.Suggestions[1].ChargeType.Should().Be("tuition");
    }

    [SkippableFact]
    public async Task SuggestAllocations_ExcessPayment_Should_CalculateRemainingUnallocatedAmount()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var (_, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);

        await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection,
            accountId: accountId,
            studentId: studentId,
            applicantId: null,
            policyId: policyId,
            chargeType: "tuition",
            description: "Single Obligation",
            amount: 800_000,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            academicPeriodId: periodId,
            isMandatory: true,
            status: "assessed");

        // Payment amount 1,500,000 exceeds obligation of 800,000
        var response = await Client.GetAsync($"/api/finance/allocations/suggest?studentId={studentId}&paymentAmount=1500000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SuggestAllocationsResponse>();
        result.Should().NotBeNull();
        result!.PaymentAmount.Should().Be(1_500_000);
        result.TotalSuggestedAllocated.Should().Be(800_000);
        result.RemainingUnallocatedAmount.Should().Be(700_000);
        result.Suggestions.Should().ContainSingle(s => s.SuggestedAmount == 800_000);
    }

    [SkippableFact]
    public async Task AllocatePayment_Should_CommitAllocations_And_MarkObligationsAsSettled()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var (_, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Allocate Test");

        // Obligation A: 1,000,000 IDR
        var oblA = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection, accountId, studentId, null, policyId, "tuition", "Obligation A", 1_000_000,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), periodId, true, "assessed");

        // Obligation B: 2,000,000 IDR
        var oblB = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection, accountId, studentId, null, policyId, "tuition", "Obligation B", 2_000_000,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), periodId, true, "assessed");

        // Confirmed Payment: 2,000,000 IDR
        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection, accountId, studentId, null, payerId, 2_000_000, "bank_transfer", "confirmed", $"REC-2026-{suffix[..6]}");

        // Act: Allocate 1,000,000 to oblA (100% full settlement) and 500,000 to oblB (partial)
        var request = new AllocatePaymentRequest(
            PaymentId: paymentId,
            Allocations:
            [
                new AllocationItemRequest(oblA, 1_000_000, "Full payment of Obligation A"),
                new AllocationItemRequest(oblB, 500_000, "Partial payment of Obligation B")
            ],
            Notes: "Counter cashier allocation batch");

        var response = await Client.PostAsJsonAsync("/api/finance/allocations", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AllocatePaymentResponse>();
        result.Should().NotBeNull();
        result!.PaymentId.Should().Be(paymentId);
        result.PaymentAmount.Should().Be(2_000_000);
        result.TotalAllocatedInRequest.Should().Be(1_500_000);
        result.CumulativeAllocatedAmount.Should().Be(1_500_000);
        result.RemainingUnallocatedAmount.Should().Be(500_000);
        result.Allocations.Should().HaveCount(2);

        // Verify Database state
        const string selectOblSql = "SELECT id AS Id, status AS Status, original_amount AS OriginalAmount FROM finance.financial_obligations WHERE id IN (@OblA, @OblB);";
        var obligations = (await QueryAsync<ObligationStatusDbRow>(selectOblSql, new { OblA = oblA, OblB = oblB })).ToList();

        // Obligation A is 100% settled -> status must transition to 'settled'
        obligations.Single(o => o.Id == oblA).Status.Should().Be("settled");

        // Obligation B is only 500,000 of 2,000,000 paid -> status remains 'assessed'
        obligations.Single(o => o.Id == oblB).Status.Should().Be("assessed");

        // Verify active rows in finance.payment_allocations
        const string selectAllocSql = "SELECT COUNT(1) FROM finance.payment_allocations WHERE payment_id = @PaymentId AND status = 'active';";
        var activeAllocCount = await ExecuteScalarAsync<int>(selectAllocSql, new { PaymentId = paymentId });
        activeAllocCount.Should().Be(2);
    }

    [SkippableFact]
    public async Task AllocatePayment_OverAllocationExceedingPaymentGross_Should_ReturnConflict()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var (_, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer OverAlloc Test");

        var oblId = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection, accountId, studentId, null, policyId, "tuition", "Big Obligation", 5_000_000,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), periodId, true, "assessed");

        // Payment gross amount is only 1,000,000
        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection, accountId, studentId, null, payerId, 1_000_000, "bank_transfer", "confirmed", $"REC-2026-{suffix[..6]}");

        // Attempting to allocate 1,500,000 from a 1,000,000 payment
        var request = new AllocatePaymentRequest(
            PaymentId: paymentId,
            Allocations: [new AllocationItemRequest(oblId, 1_500_000)]);

        var response = await Client.PostAsJsonAsync("/api/finance/allocations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task AllocatePayment_OverAllocationExceedingObligationBalance_Should_ReturnConflict()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var (_, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Oblig OverAlloc Test");

        // Obligation is only 500,000
        var oblId = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection, accountId, studentId, null, policyId, "tuition", "Small Obligation", 500_000,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), periodId, true, "assessed");

        // Payment has 2,000,000 funds
        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection, accountId, studentId, null, payerId, 2_000_000, "bank_transfer", "confirmed", $"REC-2026-{suffix[..6]}");

        // Attempting to allocate 800,000 to a 500,000 obligation
        var request = new AllocatePaymentRequest(
            PaymentId: paymentId,
            Allocations: [new AllocationItemRequest(oblId, 800_000)]);

        var response = await Client.PostAsJsonAsync("/api/finance/allocations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task ReallocatePayment_Should_MarkTargetRowsSuperseded_RecordSupersededByIdAndReversalReason_AndCreateReplacements()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var (_, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Realloc Test");

        // 1. Obligation A (1,000,000 IDR) & Obligation B (2,000,000 IDR)
        var oblA = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection, accountId, studentId, null, policyId, "tuition", "Obligation A", 1_000_000,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), periodId, true, "assessed");

        var oblB = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection, accountId, studentId, null, policyId, "tuition", "Obligation B", 2_000_000,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), periodId, true, "assessed");

        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection, accountId, studentId, null, payerId, 1_000_000, "bank_transfer", "confirmed", $"REC-2026-{suffix[..6]}");

        // 2. Perform initial allocation of 1,000,000 to Obligation A
        var initialAllocRequest = new AllocatePaymentRequest(
            PaymentId: paymentId,
            Allocations: [new AllocationItemRequest(oblA, 1_000_000)]);

        var initialAllocResponse = await Client.PostAsJsonAsync("/api/finance/allocations", initialAllocRequest);
        initialAllocResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var initialResult = await initialAllocResponse.Content.ReadFromJsonAsync<AllocatePaymentResponse>();
        var priorAllocationId = initialResult!.Allocations[0].AllocationId;

        // Verify Obligation A is 'settled'
        const string checkStatusSql = "SELECT status FROM finance.financial_obligations WHERE id = @Id;";
        var oblAStatusBefore = await ExecuteScalarAsync<string>(checkStatusSql, new { Id = oblA });
        oblAStatusBefore.Should().Be("settled");

        // 3. Reallocate: supersede allocation on Obligation A and replace with allocation to Obligation B
        const string reversalReason = "Salah akun alokasi kasir - dialihkan ke SPP bulan berjalan sesuai konfirmasi orang tua";
        var reallocRequest = new ReallocatePaymentRequest(
            PaymentId: paymentId,
            PriorAllocationIds: [priorAllocationId],
            ReplacementAllocations: [new ReplacementAllocationItemRequest(oblB, 1_000_000, "Replacement allocation to Obligation B")],
            ReversalReason: reversalReason,
            Notes: "Audited cashier reallocation");

        var reallocResponse = await Client.PostAsJsonAsync("/api/finance/allocations/reallocate", reallocRequest);
        reallocResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reallocResult = await reallocResponse.Content.ReadFromJsonAsync<ReallocatePaymentResponse>();
        reallocResult.Should().NotBeNull();
        reallocResult!.PaymentId.Should().Be(paymentId);
        reallocResult.SupersededAllocationIds.Should().ContainSingle(id => id == priorAllocationId);
        reallocResult.ReplacementAllocations.Should().ContainSingle();

        var replacementAllocation = reallocResult.ReplacementAllocations[0];
        replacementAllocation.ObligationId.Should().Be(oblB);
        replacementAllocation.AllocatedAmount.Should().Be(1_000_000);
        replacementAllocation.Status.Should().Be("active");

        // 4. Verify historical lineage on the prior allocation row in DB
        const string selectPriorAllocSql = """
            SELECT id AS Id, allocation_number AS AllocationNumber, obligation_id AS ObligationId,
                   payment_id AS PaymentId, allocated_amount AS AllocatedAmount, status AS Status,
                   superseded_by_id AS SupersededById, reversal_reason AS ReversalReason
            FROM finance.payment_allocations
            WHERE id = @Id;
            """;

        var priorRow = await QuerySingleOrDefaultAsync<AllocationDbRow>(selectPriorAllocSql, new { Id = priorAllocationId });
        priorRow.Should().NotBeNull();
        priorRow!.Status.Should().Be("superseded", "prior allocation must be marked superseded");
        priorRow.SupersededById.Should().Be(replacementAllocation.AllocationId, "prior allocation must point to replacement allocation ID");
        priorRow.ReversalReason.Should().Be(reversalReason, "reversal reason must be permanently audited");

        // 5. Verify Obligation A status reverted from 'settled' back to 'assessed'
        var oblAStatusAfter = await ExecuteScalarAsync<string>(checkStatusSql, new { Id = oblA });
        oblAStatusAfter.Should().Be("assessed", "unsettling obligation via reallocation must restore assessed status");

        // 6. Verify Obligation B active allocated amount is 1,000,000
        const string oblBPaidSql = "SELECT ISNULL(SUM(allocated_amount), 0) FROM finance.payment_allocations WHERE obligation_id = @Id AND status = 'active';";
        var oblBActivePaid = await ExecuteScalarAsync<long>(oblBPaidSql, new { Id = oblB });
        oblBActivePaid.Should().Be(1_000_000);
    }
}
