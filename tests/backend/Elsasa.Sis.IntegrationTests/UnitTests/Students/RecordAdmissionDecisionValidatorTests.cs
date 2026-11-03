namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class RecordAdmissionDecisionValidatorTests
{
    private readonly RecordAdmissionDecisionValidator _validator = new();

    [Theory]
    [InlineData("accepted")]
    [InlineData("rejected")]
    [InlineData("waitlisted")]
    [InlineData("withdrawn")]
    [InlineData("expired")]
    [InlineData("ACCEPTED")]
    [InlineData("Rejected ")]
    [InlineData("  waitlisted  ")]
    public void Validator_Should_Pass_ForValidNonDeferredDecisionTypes(string decisionType)
    {
        // Arrange
        var request = new RecordAdmissionDecisionRequest(
            decisionType,
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1),
            Reason: "Meets academic and developmental criteria.",
            SupportingReference: "REF-2026-DEC-001");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_Should_Pass_ForDeferredDecision_WhenPeriodIsProvided()
    {
        // Arrange
        var request = new RecordAdmissionDecisionRequest(
            "deferred",
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1),
            Reason: "Requested deferral to subsequent semester.",
            DeferredAdmissionPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("deferred")]
    [InlineData("DEFERRED")]
    [InlineData(" Deferred ")]
    public void Validator_Should_Fail_WhenDeferredWithoutPeriodId(string decisionType)
    {
        // Arrange
        var request = new RecordAdmissionDecisionRequest(
            decisionType,
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1),
            DeferredAdmissionPeriodId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeferredAdmissionPeriodId)
            .WithErrorMessage("Deferred admission period is required when decision type is 'deferred'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    [InlineData("invalid_status")]
    [InlineData("approved")]
    [InlineData("pending")]
    [InlineData("enrolled")]
    [InlineData("denied")]
    public void Validator_Should_Fail_ForInvalidDecisionTypes(string? decisionType)
    {
        // Arrange
        var request = new RecordAdmissionDecisionRequest(
            decisionType!,
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DecisionType);
    }

    [Fact]
    public void Validator_Should_Fail_WhenDecisionDateIsDefault()
    {
        // Arrange
        var request = new RecordAdmissionDecisionRequest(
            "accepted",
            DecisionDate: default,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DecisionDate)
            .WithErrorMessage("Decision date is required.");
    }

    [Fact]
    public void Validator_Should_Fail_WhenEffectiveDateIsDefault()
    {
        // Arrange
        var request = new RecordAdmissionDecisionRequest(
            "accepted",
            DecisionDate: new DateOnly(2026, 6, 15),
            EffectiveDate: default);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate)
            .WithErrorMessage("Effective date is required.");
    }

    [Fact]
    public void Validator_Should_Fail_WhenSupportingReferenceExceeds200Characters()
    {
        // Arrange
        var longRef = new string('A', 201);
        var request = new RecordAdmissionDecisionRequest(
            "accepted",
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1),
            SupportingReference: longRef);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SupportingReference)
            .WithErrorMessage("Supporting reference must not exceed 200 characters.");
    }
}
