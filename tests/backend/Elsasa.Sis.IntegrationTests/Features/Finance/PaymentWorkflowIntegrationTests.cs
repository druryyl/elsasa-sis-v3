namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Dapper;
using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Finance.Features.Payments.ConfirmPayment;
using Elsasa.Sis.Finance.Features.Payments.RecordAndConfirmPayment;
using Elsasa.Sis.Finance.Features.Payments.RejectPayment;
using Elsasa.Sis.Finance.Features.Payments.SubmitPayment;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration test suite verifying payment submission, cashier recording, concurrency-safe sequential receipt issuance,
/// and rejection workflows (P4-S12, OQ-004, TD-04).
/// </summary>
public sealed class PaymentWorkflowIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record PaymentDbRow(
        Guid Id,
        string PaymentNumber,
        Guid StudentAccountId,
        Guid? StudentId,
        Guid? ApplicantId,
        long PaymentAmount,
        string ConfirmationStatus,
        string? ReceiptNumber,
        DateTimeOffset? ReceiptIssuedAt,
        DateTimeOffset? ConfirmationDate,
        string? ConfirmedBy,
        string? RejectionReason);

    [SkippableFact]
    public async Task SubmitPayment_Should_CreatePaymentInSubmittedStatus_WithoutReceiptNumber()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);

        Factory.EventCollector.Reset();

        var request = new SubmitPaymentRequest(
            StudentId: studentId,
            ApplicantId: null,
            StudentAccountId: accountId,
            Amount: 2_500_000,
            PaymentMethod: "bank_transfer",
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ExternalReference: $"TRF-BCA-{suffix}",
            PayerName: "Ibu Siti Rahmawati",
            PayerPhone: "+628123456789",
            PayerEmail: $"rahmawati_{suffix}@example.local",
            Notes: "Self-service transfer proof submitted via portal");

        var response = await Client.PostAsJsonAsync("/api/finance/payments/submit", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<SubmitPaymentResponse>();
        result.Should().NotBeNull();
        result!.PaymentId.Should().NotBeEmpty();
        result.PaymentNumber.Should().StartWith($"PAY-{DateTime.UtcNow.Year}-");
        result.ConfirmationStatus.Should().Be("submitted");
        result.Amount.Should().Be(2_500_000);
        result.PaymentMethod.Should().Be("bank_transfer");

        // Verify DB state: submitted payments must NOT hold receipt numbers
        const string selectSql = """
            SELECT id AS Id, payment_number AS PaymentNumber, student_account_id AS StudentAccountId,
                   student_id AS StudentId, applicant_id AS ApplicantId, payment_amount AS PaymentAmount,
                   confirmation_status AS ConfirmationStatus, receipt_number AS ReceiptNumber,
                   receipt_issued_at AS ReceiptIssuedAt, confirmation_date AS ConfirmationDate,
                   confirmed_by AS ConfirmedBy, rejection_reason AS RejectionReason
            FROM finance.payments
            WHERE id = @Id;
            """;

        var payment = await QuerySingleOrDefaultAsync<PaymentDbRow>(selectSql, new { Id = result.PaymentId });
        payment.Should().NotBeNull();
        payment!.ConfirmationStatus.Should().Be("submitted");
        payment.ReceiptNumber.Should().BeNull("submitted portal payments must not be assigned a receipt number");
        payment.ReceiptIssuedAt.Should().BeNull();
        payment.ConfirmationDate.Should().BeNull();
        payment.ConfirmedBy.Should().BeNull();

        // Verify domain event was NOT emitted for unconfirmed submissions
        Factory.EventCollector.PaymentConfirmedEvents.Should().NotContain(e => e.PaymentId == result.PaymentId);
    }

    [SkippableFact]
    public async Task ConfirmPayment_Should_TransitionToConfirmed_AssignSequentialReceipt_AndPublishDomainEvent()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Confirm Test");

        // Seed submitted payment
        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection,
            accountId: accountId,
            studentId: studentId,
            applicantId: null,
            payerId: payerId,
            amount: 3_000_000,
            paymentMethod: "bank_transfer",
            status: "submitted",
            receiptNumber: null);

        Factory.EventCollector.Reset();

        // Cashier confirms payment
        var response = await Client.PostAsync($"/api/finance/payments/{paymentId}/confirm", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ConfirmPaymentResponse>();
        result.Should().NotBeNull();
        result!.PaymentId.Should().Be(paymentId);
        result.ConfirmationStatus.Should().Be("confirmed");
        result.ReceiptNumber.Should().MatchRegex(@"^REC-\d{4}-\d{6}$");
        result.Amount.Should().Be(3_000_000);
        result.ConfirmedBy.Should().NotBeNullOrWhiteSpace();

        // Verify DB update
        const string selectSql = """
            SELECT id AS Id, payment_number AS PaymentNumber, student_account_id AS StudentAccountId,
                   student_id AS StudentId, applicant_id AS ApplicantId, payment_amount AS PaymentAmount,
                   confirmation_status AS ConfirmationStatus, receipt_number AS ReceiptNumber,
                   receipt_issued_at AS ReceiptIssuedAt, confirmation_date AS ConfirmationDate,
                   confirmed_by AS ConfirmedBy, rejection_reason AS RejectionReason
            FROM finance.payments
            WHERE id = @Id;
            """;

        var payment = await QuerySingleOrDefaultAsync<PaymentDbRow>(selectSql, new { Id = paymentId });
        payment.Should().NotBeNull();
        payment!.ConfirmationStatus.Should().Be("confirmed");
        payment.ReceiptNumber.Should().Be(result.ReceiptNumber);
        payment.ReceiptIssuedAt.Should().NotBeNull();
        payment.ConfirmationDate.Should().NotBeNull();
        payment.ConfirmedBy.Should().NotBeNull();

        // Verify PaymentConfirmedEvent domain event was published
        Factory.EventCollector.PaymentConfirmedEvents.Should().ContainSingle(e =>
            e.PaymentId == paymentId &&
            e.ReceiptNumber == result.ReceiptNumber &&
            e.Amount == 3_000_000 &&
            e.StudentId == studentId);

        // Attempting to confirm an already confirmed payment must return 409 Conflict
        var secondConfirm = await Client.PostAsync($"/api/finance/payments/{paymentId}/confirm", null);
        secondConfirm.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task RecordAndConfirmPayment_Should_AtomicallyCreateConfirmedPayment_WithSequentialReceipt()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);

        Factory.EventCollector.Reset();

        var request = new RecordAndConfirmPaymentRequest(
            StudentId: studentId,
            ApplicantId: null,
            StudentAccountId: accountId,
            Amount: 1_750_000,
            PaymentMethod: "cash",
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ExternalReference: $"CASH-COUNTER-{suffix}",
            PayerName: "Bpk. Hendra Walk-in Parent",
            Notes: "Walk-in cashier counter payment with cash receipt");

        var response = await Client.PostAsJsonAsync("/api/finance/payments", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RecordAndConfirmPaymentResponse>();
        result.Should().NotBeNull();
        result!.PaymentId.Should().NotBeEmpty();
        result.ConfirmationStatus.Should().Be("confirmed");
        result.ReceiptNumber.Should().MatchRegex(@"^REC-\d{4}-\d{6}$");
        result.PaymentNumber.Should().StartWith($"PAY-{DateTime.UtcNow.Year}-");
        result.Amount.Should().Be(1_750_000);
        result.ConfirmedBy.Should().NotBeNullOrWhiteSpace();

        // Verify DB state
        const string selectSql = """
            SELECT id AS Id, payment_number AS PaymentNumber, student_account_id AS StudentAccountId,
                   student_id AS StudentId, applicant_id AS ApplicantId, payment_amount AS PaymentAmount,
                   confirmation_status AS ConfirmationStatus, receipt_number AS ReceiptNumber,
                   receipt_issued_at AS ReceiptIssuedAt, confirmation_date AS ConfirmationDate,
                   confirmed_by AS ConfirmedBy, rejection_reason AS RejectionReason
            FROM finance.payments
            WHERE id = @Id;
            """;

        var payment = await QuerySingleOrDefaultAsync<PaymentDbRow>(selectSql, new { Id = result.PaymentId });
        payment.Should().NotBeNull();
        payment!.ConfirmationStatus.Should().Be("confirmed");
        payment.ReceiptNumber.Should().Be(result.ReceiptNumber);

        // Verify PaymentConfirmedEvent domain event was published
        Factory.EventCollector.PaymentConfirmedEvents.Should().ContainSingle(e =>
            e.PaymentId == result.PaymentId &&
            e.ReceiptNumber == result.ReceiptNumber &&
            e.Amount == 1_750_000 &&
            e.StudentId == studentId);
    }

    [SkippableFact]
    public async Task ConfirmPayment_ConcurrentConfirmations_Should_GenerateStrictlySequentialReceiptNumbers_WithoutDuplicates()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Concurrent Test");

        const int batchCount = 10;
        var paymentIds = new List<Guid>();

        for (var i = 0; i < batchCount; i++)
        {
            var id = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
                connection,
                accountId: accountId,
                studentId: studentId,
                applicantId: null,
                payerId: payerId,
                amount: 500_000 + (i * 10_000),
                paymentMethod: "bank_transfer",
                status: "submitted",
                receiptNumber: null);

            paymentIds.Add(id);
        }

        // Execute concurrent confirmations
        var confirmTasks = paymentIds.Select(async id =>
        {
            var res = await Client.PostAsync($"/api/finance/payments/{id}/confirm", null);
            res.StatusCode.Should().Be(HttpStatusCode.OK, "concurrent confirmation must succeed without locking collisions");
            return await res.Content.ReadFromJsonAsync<ConfirmPaymentResponse>();
        });

        var results = await Task.WhenAll(confirmTasks);

        var receiptNumbers = results.Select(r => r!.ReceiptNumber).ToList();

        // 1. Verify all receipt numbers are unique
        receiptNumbers.Distinct().Should().HaveCount(batchCount, "every concurrent confirmation must produce a unique receipt number");

        // 2. Parse sequence numbers and verify strictly consecutive sequence
        var year = DateTime.UtcNow.Year;
        var prefix = $"REC-{year}-";
        var sequenceNumbers = receiptNumbers
            .Select(r =>
            {
                r.Should().StartWith(prefix);
                return int.Parse(r.AsSpan(prefix.Length), System.Globalization.CultureInfo.InvariantCulture);
            })
            .OrderBy(n => n)
            .ToList();

        for (var i = 1; i < sequenceNumbers.Count; i++)
        {
            var diff = sequenceNumbers[i] - sequenceNumbers[i - 1];
            diff.Should().Be(1, $"receipt sequence numbers must be strictly sequential (expected {sequenceNumbers[i - 1] + 1}, got {sequenceNumbers[i]})");
        }
    }

    [SkippableFact]
    public async Task RejectPayment_Should_SetRejected_WithMandatoryReason_AndNoReceipt()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: studentId);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Reject Test");

        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection,
            accountId: accountId,
            studentId: studentId,
            applicantId: null,
            payerId: payerId,
            amount: 1_000_000,
            paymentMethod: "bank_transfer",
            status: "submitted",
            receiptNumber: null);

        const string rejectionReason = "Bukti transfer tidak valid - mutasi bank tidak ditemukan";
        var request = new RejectPaymentRequest(RejectionReason: rejectionReason, Notes: "Ditolak saat rekonsiliasi kas harian");

        var response = await Client.PostAsJsonAsync($"/api/finance/payments/{paymentId}/reject", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RejectPaymentResponse>();
        result.Should().NotBeNull();
        result!.PaymentId.Should().Be(paymentId);
        result.ConfirmationStatus.Should().Be("rejected");
        result.RejectionReason.Should().Be(rejectionReason);

        // Verify DB state
        const string selectSql = """
            SELECT id AS Id, payment_number AS PaymentNumber, student_account_id AS StudentAccountId,
                   student_id AS StudentId, applicant_id AS ApplicantId, payment_amount AS PaymentAmount,
                   confirmation_status AS ConfirmationStatus, receipt_number AS ReceiptNumber,
                   receipt_issued_at AS ReceiptIssuedAt, confirmation_date AS ConfirmationDate,
                   confirmed_by AS ConfirmedBy, rejection_reason AS RejectionReason
            FROM finance.payments
            WHERE id = @Id;
            """;

        var payment = await QuerySingleOrDefaultAsync<PaymentDbRow>(selectSql, new { Id = paymentId });
        payment.Should().NotBeNull();
        payment!.ConfirmationStatus.Should().Be("rejected");
        payment.ReceiptNumber.Should().BeNull("rejected payments must not have a receipt number assigned");
        payment.RejectionReason.Should().Be(rejectionReason);

        // Attempting to confirm a rejected payment must return 409 Conflict
        var confirmResponse = await Client.PostAsync($"/api/finance/payments/{paymentId}/confirm", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Attempting to reject an already rejected payment must return 409 Conflict
        var secondReject = await Client.PostAsJsonAsync($"/api/finance/payments/{paymentId}/reject", request);
        secondReject.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task RejectPayment_WithoutReason_Should_ReturnValidationBadRequest()
    {
        SkipIfDatabaseUnavailable();

        var paymentId = Guid.NewGuid();
        var emptyRequest = new RejectPaymentRequest(RejectionReason: string.Empty);

        var response = await Client.PostAsJsonAsync($"/api/finance/payments/{paymentId}/reject", emptyRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
