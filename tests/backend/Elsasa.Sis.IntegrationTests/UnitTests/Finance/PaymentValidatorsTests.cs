namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Payments.ConfirmPayment;
using Elsasa.Sis.Finance.Features.Payments.RecordAndConfirmPayment;
using Elsasa.Sis.Finance.Features.Payments.RejectPayment;
using Elsasa.Sis.Finance.Features.Payments.SubmitPayment;
using Elsasa.Sis.Shared.Database;
using FluentValidation.TestHelper;
using MediatR;

/// <summary>
/// Unit tests for payment validators and input validation logic:
/// <see cref="SubmitPaymentValidator"/>, <see cref="RecordAndConfirmPaymentValidator"/>,
/// <see cref="RejectPaymentValidator"/>, and <see cref="ConfirmPaymentHandler"/> validation (SCR-FIN-02, TD-04, OQ-004).
/// </summary>
public sealed class PaymentValidatorsTests
{
    private readonly SubmitPaymentValidator _submitValidator = new();
    private readonly RecordAndConfirmPaymentValidator _recordValidator = new();
    private readonly RejectPaymentValidator _rejectValidator = new();

    #region SubmitPaymentValidator Tests

    [Fact]
    public void SubmitPayment_ValidRequest_WithStudentId_ShouldPassValidation()
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 2_500_000,
            PaymentMethod: "bank_transfer",
            PaymentDate: new DateOnly(2026, 11, 4),
            ExternalReference: "TRX-BCA-20261104-001",
            PayerName: "Ahmad Dahlan",
            PayerEmail: "ahmad@example.com",
            PayerPhone: "+6281234567890",
            Notes: "Tuition installment payment");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SubmitPayment_ValidRequest_WithApplicantId_ShouldPassValidation()
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: null,
            ApplicantId: Guid.NewGuid(),
            StudentAccountId: null,
            Amount: 5_000_000,
            PaymentMethod: "qris",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Siti Rahma");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SubmitPayment_ValidRequest_WithStudentAccountId_ShouldPassValidation()
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: null,
            ApplicantId: null,
            StudentAccountId: Guid.NewGuid(),
            Amount: 1_000_000,
            PaymentMethod: "cash",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Indah Permata");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500_000)]
    public void SubmitPayment_NonPositiveAmount_ShouldFailValidation(long invalidAmount)
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: invalidAmount,
            PaymentMethod: "bank_transfer",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Ahmad Dahlan");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Payment amount must be greater than zero IDR.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SubmitPayment_EmptyPaymentMethod_ShouldFailValidation(string? invalidMethod)
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: invalidMethod!,
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Ahmad Dahlan");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod)
            .WithErrorMessage("Payment method is required.");
    }

    [Fact]
    public void SubmitPayment_PaymentMethodTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: new string('M', 65),
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Ahmad Dahlan");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SubmitPayment_EmptyPayerName_ShouldFailValidation(string? invalidName)
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: "bank_transfer",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: invalidName!);

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PayerName)
            .WithErrorMessage("Payer name is required.");
    }

    [Fact]
    public void SubmitPayment_MissingCandidateIdentifier_ShouldFailValidation()
    {
        // Arrange (All candidate IDs null)
        var request = new SubmitPaymentRequest(
            StudentId: null,
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: "bank_transfer",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Ahmad Dahlan");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("At least one candidate identifier (StudentId, ApplicantId, or StudentAccountId) must be specified.");
    }

    [Fact]
    public void SubmitPayment_EmptyGuidCandidateIdentifiers_ShouldFailValidation()
    {
        // Arrange (Candidate IDs set to Guid.Empty)
        var request = new SubmitPaymentRequest(
            StudentId: Guid.Empty,
            ApplicantId: Guid.Empty,
            StudentAccountId: Guid.Empty,
            Amount: 1_000_000,
            PaymentMethod: "bank_transfer",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Ahmad Dahlan");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("At least one candidate identifier (StudentId, ApplicantId, or StudentAccountId) must be specified.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    public void SubmitPayment_InvalidPayerEmail_ShouldFailValidation(string invalidEmail)
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: "bank_transfer",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Ahmad Dahlan",
            PayerEmail: invalidEmail);

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PayerEmail)
            .WithErrorMessage("Payer email must be a valid email address.");
    }

    [Fact]
    public void SubmitPayment_ExternalReferenceTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new SubmitPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: "bank_transfer",
            PaymentDate: null,
            ExternalReference: new string('R', 101),
            PayerName: "Ahmad Dahlan");

        // Act
        var result = _submitValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExternalReference);
    }

    #endregion

    #region RecordAndConfirmPaymentValidator Tests

    [Fact]
    public void RecordAndConfirmPayment_ValidRequest_WithStudentId_ShouldPassValidation()
    {
        // Arrange
        var request = new RecordAndConfirmPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 3_500_000,
            PaymentMethod: "cashier_counter",
            PaymentDate: new DateOnly(2026, 11, 4),
            ExternalReference: "RCP-MANUAL-001",
            PayerName: "Dewi Lestari",
            PayerEmail: "dewi@example.com",
            PayerPhone: "+6281987654321",
            Notes: "Counter cash payment received");

        // Act
        var result = _recordValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RecordAndConfirmPayment_ValidRequest_WithApplicantId_ShouldPassValidation()
    {
        // Arrange
        var request = new RecordAndConfirmPaymentRequest(
            StudentId: null,
            ApplicantId: Guid.NewGuid(),
            StudentAccountId: null,
            Amount: 6_000_000,
            PaymentMethod: "edc_debit",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Dewi Lestari");

        // Act
        var result = _recordValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100_000)]
    public void RecordAndConfirmPayment_NonPositiveAmount_ShouldFailValidation(long invalidAmount)
    {
        // Arrange
        var request = new RecordAndConfirmPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: invalidAmount,
            PaymentMethod: "cashier_counter",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Dewi Lestari");

        // Act
        var result = _recordValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Payment amount must be greater than zero IDR.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordAndConfirmPayment_EmptyPaymentMethod_ShouldFailValidation(string? invalidMethod)
    {
        // Arrange
        var request = new RecordAndConfirmPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: invalidMethod!,
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Dewi Lestari");

        // Act
        var result = _recordValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod)
            .WithErrorMessage("Payment method is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordAndConfirmPayment_EmptyPayerName_ShouldFailValidation(string? invalidName)
    {
        // Arrange
        var request = new RecordAndConfirmPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: "cashier_counter",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: invalidName!);

        // Act
        var result = _recordValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PayerName)
            .WithErrorMessage("Payer name is required.");
    }

    [Fact]
    public void RecordAndConfirmPayment_MissingCandidateIdentifier_ShouldFailValidation()
    {
        // Arrange
        var request = new RecordAndConfirmPaymentRequest(
            StudentId: null,
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: "cashier_counter",
            PaymentDate: null,
            ExternalReference: null,
            PayerName: "Dewi Lestari");

        // Act
        var result = _recordValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("At least one candidate identifier (StudentId, ApplicantId, or StudentAccountId) must be specified.");
    }

    [Fact]
    public void RecordAndConfirmPayment_InvalidEmailAndLongReference_ShouldFailValidation()
    {
        // Arrange
        var request = new RecordAndConfirmPaymentRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            StudentAccountId: null,
            Amount: 1_000_000,
            PaymentMethod: "cashier_counter",
            PaymentDate: null,
            ExternalReference: new string('Z', 101),
            PayerName: "Dewi Lestari",
            PayerEmail: "invalid-email-address");

        // Act
        var result = _recordValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PayerEmail)
            .WithErrorMessage("Payer email must be a valid email address.");
        result.ShouldHaveValidationErrorFor(x => x.ExternalReference);
    }

    #endregion

    #region RejectPaymentValidator Tests

    [Fact]
    public void RejectPayment_ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new RejectPaymentRequest(
            RejectionReason: "Proof of transfer image is illegible; bank statement reference not found.",
            Notes: "Cashier requested applicant re-submit valid proof");

        // Act
        var result = _rejectValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectPayment_EmptyRejectionReason_ShouldFailValidation(string? invalidReason)
    {
        // Arrange
        var request = new RejectPaymentRequest(RejectionReason: invalidReason!);

        // Act
        var result = _rejectValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RejectionReason)
            .WithErrorMessage("Rejection reason is mandatory.");
    }

    [Fact]
    public void RejectPayment_RejectionReasonExceeding1000Chars_ShouldFailValidation()
    {
        // Arrange
        var longReason = new string('R', 1001);
        var request = new RejectPaymentRequest(RejectionReason: longReason);

        // Act
        var result = _rejectValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RejectionReason)
            .WithErrorMessage("Rejection reason cannot exceed 1000 characters.");
    }

    #endregion

    #region ConfirmPayment Validation Tests

    [Fact]
    public async Task ConfirmPayment_EmptyPaymentId_ShouldReturnValidationError()
    {
        // Arrange
        var connectionFactory = new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;");
        var publisher = new StubPublisher();
        var handler = new ConfirmPaymentHandler(connectionFactory, publisher);

        // Act
        var result = await handler.HandleAsync(Guid.Empty);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Finance.Payment.InvalidId", result.Error.Code);
        Assert.Equal("Payment ID cannot be empty.", result.Error.Message);
    }

    private sealed class StubPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return Task.CompletedTask;
        }
    }

    #endregion
}
