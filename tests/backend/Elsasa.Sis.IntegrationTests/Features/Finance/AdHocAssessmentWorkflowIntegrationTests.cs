namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;
using Elsasa.Sis.Finance.Features.Obligations.GetObligations;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration tests verifying ad-hoc obligation assessment workflows (P4-S12, P2-S05, GAP-002, GAP-003, TD-04).
/// Verifies that ad-hoc assessments create sequence-1 due schedules, auto-provision missing student accounts,
/// dispatch domain events, and are queryable.
/// </summary>
public sealed class AdHocAssessmentWorkflowIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record ScheduleDbRow(
        Guid Id,
        Guid ObligationId,
        int InstallmentSequence,
        DateOnly DueDate,
        long ScheduledAmount,
        string Status);

    [SkippableFact]
    public async Task AssessAdHocObligation_Should_AutoProvisionMissingAccount_And_CreateSequence1Schedule()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix, "active");
        var (_, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);

        // Precondition check: verify student currently has NO account in finance.student_financial_accounts
        const string checkAccountSql = "SELECT COUNT(1) FROM finance.student_financial_accounts WHERE student_id = @StudentId;";
        var initialAccountCount = await ExecuteScalarAsync<int>(checkAccountSql, new { StudentId = studentId });
        initialAccountCount.Should().Be(0, "precondition: student must not have an account prior to ad-hoc assessment");

        Factory.EventCollector.Reset();

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        var request = new AssessAdHocObligationRequest(
            StudentId: studentId,
            ChargeType: "library_fee",
            Description: "Denda Penggantian Buku Perpustakaan",
            Amount: 150000,
            DueDate: dueDate,
            AcademicPeriodId: periodId,
            Notes: "Ad-hoc assessment auto-provisioning test");

        var response = await Client.PostAsJsonAsync("/api/finance/obligations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AssessAdHocObligationResponse>();
        result.Should().NotBeNull();
        result!.StudentId.Should().Be(studentId);
        result.OriginalAmount.Should().Be(150000);
        result.Status.Should().Be("assessed");
        result.CurrencyCode.Should().Be("IDR");
        result.DueDate.Should().Be(dueDate);
        result.StudentAccountId.Should().NotBeEmpty();

        // 1. Verify account was automatically provisioned
        var postAccountCount = await ExecuteScalarAsync<int>(checkAccountSql, new { StudentId = studentId });
        postAccountCount.Should().Be(1, "ad-hoc assessment must auto-provision missing student financial account");

        // 2. Verify sequence-1 due schedule in database
        const string selectScheduleSql = """
            SELECT id AS Id, obligation_id AS ObligationId, installment_sequence AS InstallmentSequence,
                   due_date AS DueDate, scheduled_amount AS ScheduledAmount, status AS Status
            FROM finance.obligation_due_schedules
            WHERE obligation_id = @ObligationId;
            """;

        var schedule = await QuerySingleOrDefaultAsync<ScheduleDbRow>(selectScheduleSql, new { ObligationId = result.Id });
        schedule.Should().NotBeNull();
        schedule!.InstallmentSequence.Should().Be(1);
        schedule.ScheduledAmount.Should().Be(150000);
        schedule.DueDate.Should().Be(dueDate);
        schedule.Status.Should().Be("pending");

        // 3. Verify ObligationAssessedEvent domain event was published
        Factory.EventCollector.ObligationAssessedEvents.Should().ContainSingle(e =>
            e.ObligationId == result.Id &&
            e.StudentId == studentId &&
            e.Amount == 150000);

        // 4. Query obligations list endpoint to ensure visibility
        var queryResponse = await Client.GetAsync($"/api/finance/obligations?studentId={studentId}");
        queryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseList = await queryResponse.Content.ReadFromJsonAsync<GetObligationsResponse>();
        responseList.Should().NotBeNull();
        responseList!.Items.Should().ContainSingle(o => o.Id == result.Id && o.Status == "assessed");
    }

    [SkippableFact]
    public async Task AssessAdHocObligation_WithExistingAccount_Should_ReuseExistingAccount()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix, "active");
        var (ayId, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);

        // Explicitly provision account first via StudentEnrolledEvent
        using var scope = Factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<MediatR.IPublisher>();
        await publisher.Publish(new Elsasa.Sis.Contracts.Students.Events.StudentEnrolledEvent(studentId, Guid.NewGuid(), ayId));

        const string selectAccountIdSql = "SELECT id FROM finance.student_financial_accounts WHERE student_id = @StudentId;";
        var existingAccountId = await ExecuteScalarAsync<Guid>(selectAccountIdSql, new { StudentId = studentId });
        existingAccountId.Should().NotBeEmpty();

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var request = new AssessAdHocObligationRequest(
            StudentId: studentId,
            ChargeType: "uniform_fee",
            Description: "Seragam Olahraga Tambahan",
            Amount: 350000,
            DueDate: dueDate,
            AcademicPeriodId: periodId,
            Notes: "Existing account reuse test");

        var response = await Client.PostAsJsonAsync("/api/finance/obligations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AssessAdHocObligationResponse>();
        result.Should().NotBeNull();
        result!.StudentAccountId.Should().Be(existingAccountId, "ad-hoc assessment must attach to existing student account");

        // Verify account count remains 1
        const string checkAccountSql = "SELECT COUNT(1) FROM finance.student_financial_accounts WHERE student_id = @StudentId;";
        var count = await ExecuteScalarAsync<int>(checkAccountSql, new { StudentId = studentId });
        count.Should().Be(1);
    }
}
