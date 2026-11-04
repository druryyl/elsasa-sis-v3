namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class ChangeStudentStatusValidatorTests
{
    private readonly ChangeStudentStatusValidator _validator = new();

    private static ChangeStudentStatusRequest CreateValidRequest(
        string status = "active",
        DateOnly? effectiveDate = null,
        string? reason = "Administrative activation",
        string? authorizedBy = "admin-user",
        string? decisionReference = "DEC-2026-002")
    {
        return new ChangeStudentStatusRequest(
            Status: status,
            EffectiveDate: effectiveDate ?? new DateOnly(2026, 9, 1),
            Reason: reason,
            AuthorizedBy: authorizedBy,
            DecisionReference: decisionReference);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("enrolled")]
    [InlineData("Active")]
    [InlineData("ACTIVE")]
    [InlineData("Enrolled")]
    [InlineData("ENROLLED")]
    [InlineData(" active ")]
    [InlineData(" enrolled ")]
    public void Validate_Should_Pass_When_StatusIsAllowed(string status)
    {
        // Arrange
        var request = CreateValidRequest(status: status);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_Should_Pass_When_OptionalFieldsAreNull()
    {
        // Arrange
        var request = CreateValidRequest(
            reason: null,
            authorizedBy: null,
            decisionReference: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_StatusIsEmptyOrNull(string? status)
    {
        // Arrange
        var request = CreateValidRequest(status: status!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("withdrawn")]
    [InlineData("graduated")]
    [InlineData("suspended")]
    [InlineData("expelled")]
    [InlineData("pending")]
    [InlineData("invalid_status")]
    public void Validate_Should_Fail_When_StatusIsNotSupported(string status)
    {
        // Arrange
        var request = CreateValidRequest(status: status);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage("Status must be one of: active, enrolled.");
    }

    [Fact]
    public void Validate_Should_Fail_When_EffectiveDateIsDefault()
    {
        // Arrange
        var request = CreateValidRequest(effectiveDate: default(DateOnly));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Fact]
    public void Validate_Should_Fail_When_AuthorizedByExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(authorizedBy: new string('x', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AuthorizedBy)
            .WithErrorMessage("AuthorizedBy cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_DecisionReferenceExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(decisionReference: new string('y', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DecisionReference)
            .WithErrorMessage("DecisionReference cannot exceed 100 characters.");
    }
}
