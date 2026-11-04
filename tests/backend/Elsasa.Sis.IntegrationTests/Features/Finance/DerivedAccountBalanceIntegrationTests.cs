namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;
using Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration tests verifying real-time dynamic balance derivation (P4-S12, P3-S08, GAP-002, TD-02, ADR-002, ADR-003).
/// Verifies that student account balance is derived strictly on-the-fly from financial obligations without any denormalized balance column.
/// </summary>
public sealed class DerivedAccountBalanceIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [SkippableFact]
    public async Task DerivedAccountBalance_Should_DynamicallyReflectAssessed_Settled_And_CancelledObligations()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix, "active");
        var (ayId, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);

        // 1. Provision account via StudentEnrolledEvent
        using (var scope = Factory.Services.CreateScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            await publisher.Publish(new StudentEnrolledEvent(studentId, Guid.NewGuid(), ayId));
        }

        // 2. Query initial balance: should be strictly 0
        var initialResponse = await Client.GetAsync($"/api/finance/accounts/{studentId}");
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialDossier = await initialResponse.Content.ReadFromJsonAsync<GetStudentFinancialAccountResponse>();
        initialDossier.Should().NotBeNull();
        initialDossier!.TotalAssessed.Should().Be(0);
        initialDossier.TotalSettled.Should().Be(0);
        initialDossier.CurrentBalance.Should().Be(0);
        initialDossier.OpenObligations.Should().BeEmpty();

        // 3. Assess two obligations: 1,000,000 IDR and 500,000 IDR
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        var obl1Response = await Client.PostAsJsonAsync("/api/finance/obligations", new AssessAdHocObligationRequest(
            StudentId: studentId,
            ChargeType: "tuition_fee",
            Description: "SPP Semester 1",
            Amount: 1000000,
            DueDate: dueDate,
            AcademicPeriodId: periodId));
        obl1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var obl1 = await obl1Response.Content.ReadFromJsonAsync<AssessAdHocObligationResponse>();

        var obl2Response = await Client.PostAsJsonAsync("/api/finance/obligations", new AssessAdHocObligationRequest(
            StudentId: studentId,
            ChargeType: "lab_fee",
            Description: "Biaya Praktikum Komputer",
            Amount: 500000,
            DueDate: dueDate,
            AcademicPeriodId: periodId));
        obl2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var obl2 = await obl2Response.Content.ReadFromJsonAsync<AssessAdHocObligationResponse>();

        // 4. Query balance with open obligations: 1,500,000 IDR
        var assessedResponse = await Client.GetAsync($"/api/finance/accounts/{studentId}");
        assessedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var assessedDossier = await assessedResponse.Content.ReadFromJsonAsync<GetStudentFinancialAccountResponse>();
        assessedDossier.Should().NotBeNull();
        assessedDossier!.TotalAssessed.Should().Be(1500000);
        assessedDossier.TotalSettled.Should().Be(0);
        assessedDossier.CurrentBalance.Should().Be(1500000);
        assessedDossier.OpenObligations.Should().HaveCount(2);

        // 5. Simulate payment settlement of obligation 1
        await ExecuteAsync(
            "UPDATE finance.financial_obligations SET status = 'settled', updated_at = SYSUTCDATETIME() WHERE id = @Id;",
            new { Id = obl1!.Id });

        var partialSettledResponse = await Client.GetAsync($"/api/finance/accounts/{studentId}");
        partialSettledResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var partialSettledDossier = await partialSettledResponse.Content.ReadFromJsonAsync<GetStudentFinancialAccountResponse>();
        partialSettledDossier.Should().NotBeNull();
        partialSettledDossier!.TotalAssessed.Should().Be(1500000);
        partialSettledDossier.TotalSettled.Should().Be(1000000);
        partialSettledDossier.CurrentBalance.Should().Be(500000);
        // Only remaining open obligation (obl2) is listed
        partialSettledDossier.OpenObligations.Should().ContainSingle(o => o.Id == obl2!.Id);

        // 6. Simulate cancellation of obligation 2
        await ExecuteAsync(
            "UPDATE finance.financial_obligations SET status = 'cancelled', updated_at = SYSUTCDATETIME() WHERE id = @Id;",
            new { Id = obl2!.Id });

        var finalResponse = await Client.GetAsync($"/api/finance/accounts/{studentId}");
        finalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalDossier = await finalResponse.Content.ReadFromJsonAsync<GetStudentFinancialAccountResponse>();
        finalDossier.Should().NotBeNull();
        // Cancelled obligation is excluded from TotalAssessed
        finalDossier!.TotalAssessed.Should().Be(1000000);
        finalDossier.TotalSettled.Should().Be(1000000);
        finalDossier.CurrentBalance.Should().Be(0);
        finalDossier.OpenObligations.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task StudentFinancialAccountsTable_Should_HaveNoDenormalizedBalanceColumn()
    {
        SkipIfDatabaseUnavailable();

        const string checkBalanceColumnsSql = """
            SELECT COUNT(1)
            FROM sys.columns c
            INNER JOIN sys.tables t ON t.object_id = c.object_id
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = 'finance'
              AND t.name = 'student_financial_accounts'
              AND c.name IN ('balance', 'current_balance', 'total_balance', 'outstanding_balance');
            """;

        var balanceColumnCount = await ExecuteScalarAsync<int>(checkBalanceColumnsSql);
        balanceColumnCount.Should().Be(0, "ADR-002: finance.student_financial_accounts must NOT possess a mutable balance column");
    }
}
