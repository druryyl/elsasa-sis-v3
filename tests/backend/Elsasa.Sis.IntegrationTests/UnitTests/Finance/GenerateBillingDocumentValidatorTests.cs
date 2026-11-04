namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;

public sealed class GenerateBillingDocumentValidatorTests
{
    private readonly GenerateBillingDocumentValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_WithAllFields_ShouldPassValidation()
    {
        // Arrange
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.NewGuid(),
            ObligationIds: [Guid.NewGuid(), Guid.NewGuid()],
            PaymentDueDate: new DateOnly(2026, 12, 15),
            Notes: "November tuition and uniform bill");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidRequest_WithMinimalFields_ShouldPassValidation()
    {
        // Arrange
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyStudentId_ShouldFailValidation()
    {
        // Arrange
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.Empty,
            PaymentDueDate: new DateOnly(2026, 12, 15));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.StudentId));
    }

    [Fact]
    public void Validate_DefaultPaymentDueDate_ShouldFailValidation()
    {
        // Arrange
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.NewGuid(),
            PaymentDueDate: default(DateOnly));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.PaymentDueDate));
    }

    [Fact]
    public void Validate_ValidFuturePaymentDueDate_ShouldPassValidation()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.NewGuid(),
            PaymentDueDate: futureDate);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NotesExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.NewGuid(),
            PaymentDueDate: new DateOnly(2026, 12, 15),
            Notes: new string('N', 1001));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Notes));
    }
}
