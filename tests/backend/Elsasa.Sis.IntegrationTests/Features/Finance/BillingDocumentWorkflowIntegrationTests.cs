namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;
using Elsasa.Sis.Finance.Features.Billing.GenerateStatement;
using Elsasa.Sis.Finance.Features.Billing.GetBillingDocumentById;
using Elsasa.Sis.Finance.Features.Billing.GetBillingDocuments;
using Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Shared.Common;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration tests verifying billing document compilation and financial statements (P4-S12, P3-S07, GAP-005, TD-05, ADR-002, ADR-006).
/// Verifies that open obligations are compiled into an immutable bill, primary financial guarantor is resolved from the student domain,
/// line items are frozen in billing_document_items, and obligations status transitions to issued.
/// </summary>
public sealed class BillingDocumentWorkflowIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record ItemDbRow(
        Guid Id,
        Guid BillingDocumentId,
        int LineSequence,
        string ItemType,
        string ReferenceEntityType,
        Guid ReferenceEntityId,
        string Description,
        long Amount,
        string CurrencyCode);

    [SkippableFact]
    public async Task GenerateBillingDocument_Should_CompileOpenObligations_ResolveFinancialGuarantor_And_FreezeLineItems()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix, "active");
        var (ayId, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);

        // 1. Seed primary financial guarantor in student domain
        const string guarantorName = "Ny. Kartini";
        const string guarantorPhone = "+628119876543";
        const string guarantorEmail = "kartini@example.com";
        await FinanceIntegrationTestHelpers.SeedGuardianAsync(
            connection,
            studentId,
            fullName: guarantorName,
            relationshipType: "Mother",
            isFinancialContact: true,
            isPrimaryContact: true,
            phone: guarantorPhone,
            email: guarantorEmail);

        // 2. Provision financial account
        using (var scope = Factory.Services.CreateScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            await publisher.Publish(new StudentEnrolledEvent(studentId, Guid.NewGuid(), ayId));
        }

        // 3. Assess two open obligations: 800,000 IDR and 200,000 IDR
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));

        var obl1Response = await Client.PostAsJsonAsync("/api/finance/obligations", new AssessAdHocObligationRequest(
            StudentId: studentId,
            ChargeType: "tuition_fee",
            Description: "SPP Bulan Berjalan",
            Amount: 800000,
            DueDate: dueDate,
            AcademicPeriodId: periodId));
        obl1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var obl1 = await obl1Response.Content.ReadFromJsonAsync<AssessAdHocObligationResponse>();

        var obl2Response = await Client.PostAsJsonAsync("/api/finance/obligations", new AssessAdHocObligationRequest(
            StudentId: studentId,
            ChargeType: "material_fee",
            Description: "Bahan Belajar & Praktikum",
            Amount: 200000,
            DueDate: dueDate,
            AcademicPeriodId: periodId));
        obl2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var obl2 = await obl2Response.Content.ReadFromJsonAsync<AssessAdHocObligationResponse>();

        // 4. Compile open obligations into a bill
        var generateRequest = new GenerateBillingDocumentRequest(
            StudentId: studentId,
            ObligationIds: null, // null compiles all open obligations
            PaymentDueDate: dueDate,
            Notes: "Tagihan Semester Ganjil");

        var generateResponse = await Client.PostAsJsonAsync("/api/finance/billing-documents/generate", generateRequest);
        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var bill = await generateResponse.Content.ReadFromJsonAsync<GenerateBillingDocumentResponse>();
        bill.Should().NotBeNull();
        bill!.DocumentType.Should().Be("bill");
        bill.DocumentNumber.Should().StartWith($"BIL-{DateTime.UtcNow:yyyy}-");
        bill.TotalBilledAmount.Should().Be(1000000);
        bill.PaymentDueDate.Should().Be(dueDate);

        // Verify primary guarantor resolution from Student domain (GAP-005, TD-05)
        bill.RecipientName.Should().Be(guarantorName);
        bill.RecipientRelationship.Should().Be("Mother");
        bill.RecipientContact.Should().Be(guarantorPhone);

        // Verify frozen items returned in response
        bill.Items.Should().HaveCount(2);
        bill.Items.Should().OnlyContain(i => i.ItemType == "obligation" && i.ReferenceEntityType == "financial_obligations");
        bill.Items.Select(i => i.ReferenceEntityId).Should().BeEquivalentTo([obl1!.Id, obl2!.Id]);
        bill.Items.Select(i => i.LineSequence).Should().BeEquivalentTo([1, 2]);

        // 5. Verify database persistence and obligation status update
        const string selectObligationStatusSql = """
            SELECT status
            FROM finance.financial_obligations
            WHERE id IN @Ids;
            """;

        var obligationStatuses = (await QueryAsync<string>(selectObligationStatusSql, new { Ids = new[] { obl1!.Id, obl2!.Id } })).ToList();
        obligationStatuses.Should().HaveCount(2);
        obligationStatuses.Should().OnlyContain(s => s == "issued", "compiling into bill must transition obligation status to 'issued'");

        // 6. Verify frozen line items persisted in finance.billing_document_items
        const string selectItemsSql = """
            SELECT id AS Id, billing_document_id AS BillingDocumentId, line_sequence AS LineSequence,
                   item_type AS ItemType, reference_entity_type AS ReferenceEntityType,
                   reference_entity_id AS ReferenceEntityId, description AS Description,
                   amount AS Amount, currency_code AS CurrencyCode
            FROM finance.billing_document_items
            WHERE billing_document_id = @BillId
            ORDER BY line_sequence ASC;
            """;

        var dbItems = (await QueryAsync<ItemDbRow>(selectItemsSql, new { BillId = bill.Id })).ToList();
        dbItems.Should().HaveCount(2);
        dbItems[0].LineSequence.Should().Be(1);
        dbItems[1].LineSequence.Should().Be(2);
        dbItems.Sum(i => i.Amount).Should().Be(1000000);

        // 7. Verify immutability: attempting to generate bill again with no open obligations returns Conflict
        var secondGenerateResponse = await Client.PostAsJsonAsync("/api/finance/billing-documents/generate", generateRequest);
        secondGenerateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict, "no open obligations remain to compile");

        // 8. Generate periodic financial statement across date range
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var statementRequest = new GenerateStatementRequest(
            StudentId: studentId,
            BillingPeriodStart: today.AddMonths(-1),
            BillingPeriodEnd: today.AddMonths(1),
            Notes: "Laporan Keuangan Bulanan");

        var statementResponse = await Client.PostAsJsonAsync("/api/finance/billing-documents/statement", statementRequest);
        statementResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var statement = await statementResponse.Content.ReadFromJsonAsync<GenerateBillingDocumentResponse>();
        statement.Should().NotBeNull();
        statement!.DocumentType.Should().Be("statement");
        statement.DocumentNumber.Should().StartWith($"STM-{DateTime.UtcNow:yyyy}-");
        statement.RecipientName.Should().Be(guarantorName);
        statement.Items.Should().NotBeEmpty();

        // 9. Query single billing document by ID
        var getByIdResponse = await Client.GetAsync($"/api/finance/billing-documents/{bill.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var billDetail = await getByIdResponse.Content.ReadFromJsonAsync<BillingDocumentDetailResponse>();
        billDetail.Should().NotBeNull();
        billDetail!.Id.Should().Be(bill.Id);
        billDetail.DocumentNumber.Should().Be(bill.DocumentNumber);
        billDetail.Items.Should().HaveCount(2);

        // 10. Query billing documents listing endpoint
        var listResponse = await Client.GetAsync($"/api/finance/billing-documents?studentId={studentId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedList<BillingDocumentListItemDto>>();
        list.Should().NotBeNull();
        list!.Items.Should().HaveCount(2, "should contain both the generated bill and statement");
    }
}
