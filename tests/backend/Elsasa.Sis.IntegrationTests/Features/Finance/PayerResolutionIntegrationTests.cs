namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using Dapper;
using Elsasa.Sis.Finance.Features.Payers.CreatePayer;
using Elsasa.Sis.Finance.Features.Payers.GetPayerById;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration test suite verifying Payer Resolution, auto-matching, and sponsor provisioning (P4-S12, GAP-005, OQ-002, TD-03).
/// </summary>
public sealed class PayerResolutionIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record PayerDbRecord(
        Guid Id,
        string PayerNumber,
        string PayerName,
        string PayerType,
        string? IdentificationNumber,
        string? Email,
        string? PhoneNumber,
        string? CreatedBy);

    [SkippableFact]
    public async Task PayerResolver_AutoMatch_ByNik_Should_ReturnExistingPayer_WithoutDuplicateInsert()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var nik = $"3171{suffix}";
        var payerNumber = $"PYR-2026-{suffix[..4]}";

        var existingId = await FinanceIntegrationTestHelpers.SeedPayerAsync(
            connection,
            payerNumber: payerNumber,
            payerName: "Existing Parent One",
            payerType: "parent_guardian",
            idNumber: nik,
            email: $"p1_{suffix}@example.com",
            phone: $"+62811{suffix}");

        using var scope = Factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IPayerResolver>();

        // Act: try resolve using matching NIK but different name/email/phone
        var resolvedId = await resolver.ResolveOrCreatePayerAsync(
            payerName: "Different Payer Name",
            identificationNumber: nik,
            email: $"different_{suffix}@example.com",
            phone: $"+62899{suffix}",
            payerType: "parent_guardian");

        // Assert: resolved ID must match existing without inserting a duplicate
        resolvedId.Should().Be(existingId);

        const string countSql = "SELECT COUNT(1) FROM finance.payers WHERE identification_number = @Nik;";
        var count = await ExecuteScalarAsync<int>(countSql, new { Nik = nik });
        count.Should().Be(1, "auto-matching by NIK must prevent duplicate payer records");
    }

    [SkippableFact]
    public async Task PayerResolver_AutoMatch_ByEmail_Should_ReturnExistingPayer()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"payer_{suffix}@elsasa-test.local";
        var payerNumber = $"PYR-2026-{suffix[..4]}";

        var existingId = await FinanceIntegrationTestHelpers.SeedPayerAsync(
            connection,
            payerNumber: payerNumber,
            payerName: "Payer By Email",
            payerType: "parent_guardian",
            idNumber: null,
            email: email,
            phone: null);

        using var scope = Factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IPayerResolver>();

        // Act: try resolve using matching email with null identification number
        var resolvedId = await resolver.ResolveOrCreatePayerAsync(
            payerName: "Payer By Email Match",
            identificationNumber: null,
            email: email,
            phone: null,
            payerType: null);

        resolvedId.Should().Be(existingId);
    }

    [SkippableFact]
    public async Task PayerResolver_AutoMatch_ByPhone_Should_ReturnExistingPayer()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var phone = $"+62812{suffix}";
        var payerNumber = $"PYR-2026-{suffix[..4]}";

        var existingId = await FinanceIntegrationTestHelpers.SeedPayerAsync(
            connection,
            payerNumber: payerNumber,
            payerName: "Payer By Phone",
            payerType: "parent_guardian",
            idNumber: null,
            email: null,
            phone: phone);

        using var scope = Factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IPayerResolver>();

        // Act: try resolve using matching phone
        var resolvedId = await resolver.ResolveOrCreatePayerAsync(
            payerName: "Payer By Phone Match",
            identificationNumber: null,
            email: null,
            phone: phone,
            payerType: null);

        resolvedId.Should().Be(existingId);
    }

    [SkippableFact]
    public async Task PayerResolver_WhenNoMatchExists_Should_AutoCreateParentGuardianPayer()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var nik = $"3201{suffix}";
        var email = $"brandnew_{suffix}@example.com";
        var phone = $"+62877{suffix}";
        const string payerName = "Bambang Pamungkas";

        using var scope = Factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IPayerResolver>();

        // Act: resolve for a completely new identity
        var newPayerId = await resolver.ResolveOrCreatePayerAsync(
            payerName: payerName,
            identificationNumber: nik,
            email: email,
            phone: phone,
            payerType: null); // Null should default to 'parent_guardian'

        newPayerId.Should().NotBeEmpty();

        const string selectSql = """
            SELECT id AS Id, payer_number AS PayerNumber, payer_name AS PayerName,
                   payer_type AS PayerType, identification_number AS IdentificationNumber,
                   email AS Email, phone_number AS PhoneNumber, created_by AS CreatedBy
            FROM finance.payers
            WHERE id = @Id;
            """;

        var payer = await QuerySingleOrDefaultAsync<PayerDbRecord>(selectSql, new { Id = newPayerId });
        payer.Should().NotBeNull();
        payer!.PayerName.Should().Be(payerName);
        payer.PayerType.Should().Be("parent_guardian", "unspecified payer type must default to 'parent_guardian'");
        payer.PayerNumber.Should().StartWith($"PYR-{DateTime.UtcNow.Year}-");
        payer.IdentificationNumber.Should().Be(nik);
        payer.Email.Should().Be(email);
        payer.PhoneNumber.Should().Be(phone);
        payer.CreatedBy.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task CreatePayerEndpoint_Should_AllowExplicitSponsorCreation()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new CreatePayerRequest(
            Name: $"Yayasan Beasiswa Prestasi {suffix}",
            Type: "sponsor",
            IdentificationNumber: $"01.999.{suffix}.1-091.000",
            Email: $"beasiswa_{suffix}@yayasan.or.id",
            Phone: $"+6221{suffix[..6]}",
            BillingAddress: "Gedung Sentra Beasiswa Lt. 5, Jakarta Selatan",
            BankAccountName: $"Yayasan Beasiswa Prestasi {suffix}",
            BankAccountNumber: "8881234567",
            BankName: "Bank Central Asia (BCA)",
            Notes: "Institutional partner sponsoring full tuition");

        var response = await Client.PostAsJsonAsync("/api/finance/payers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreatePayerResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.PayerName.Should().Be(request.Name);
        result.PayerType.Should().Be("sponsor");
        result.PayerNumber.Should().StartWith($"PYR-{DateTime.UtcNow.Year}-");

        // Verify retrieval via GET /api/finance/payers/{id}
        var getResponse = await Client.GetAsync($"/api/finance/payers/{result.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getResponse.Content.ReadFromJsonAsync<GetPayerByIdResponse>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(result.Id);
        detail.PayerType.Should().Be("sponsor");
        detail.BankAccountName.Should().Be(request.BankAccountName);
        detail.BankAccountNumber.Should().Be(request.BankAccountNumber);
        detail.BankName.Should().Be(request.BankName);
        detail.BillingAddress.Should().Be(request.BillingAddress);
    }

    [SkippableFact]
    public async Task CreatePayerEndpoint_DuplicateIdentificationNumber_Should_ReturnConflict()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var idNumber = $"NPWP-{suffix}";
        var payerNumber = $"PYR-2026-{suffix[..4]}";

        await FinanceIntegrationTestHelpers.SeedPayerAsync(
            connection,
            payerNumber: payerNumber,
            payerName: "Original Corporate Sponsor",
            payerType: "sponsor",
            idNumber: idNumber);

        var duplicateRequest = new CreatePayerRequest(
            Name: "Duplicate Sponsor Attempt",
            Type: "sponsor",
            IdentificationNumber: idNumber);

        var response = await Client.PostAsJsonAsync("/api/finance/payers", duplicateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
