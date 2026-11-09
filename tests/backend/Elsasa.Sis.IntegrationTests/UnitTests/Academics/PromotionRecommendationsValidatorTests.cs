namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.PromotionRecommendations.BatchIssuePromotionRecommendations;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.IssuePromotionRecommendation;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

/// <summary>
/// Unit test suite for single and batch promotion recommendation issuance command validators (P3-S08, UC-ACD-17).
/// Verifies required fields, recommendation enum constraints, conditional promotion rules, and committee exception requirements.
/// </summary>
public sealed class PromotionRecommendationsValidatorTests
{
    private readonly IssuePromotionRecommendationValidator _singleValidator = new();
    private readonly BatchIssuePromotionRecommendationsValidator _batchValidator = new();
    private readonly ProgressionItemDtoValidator _itemValidator = new();

    #region Single IssuePromotionRecommendationValidator Tests

    [Fact]
    public void SingleValidator_ValidPromotionEligible_ShouldPass()
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: "Passed all 8 enrolled learning units with GPA 3.75.",
            AcademicStandingId: Guid.NewGuid(),
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SingleValidator_ValidConditionalPromotion_WithConditions_ShouldPass()
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "conditional_promotion",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: "Must complete Mathematics remediation packet by beginning of term.",
            SupportingEvidence: "Conditional standing due to 1 failed unit in Mathematics.",
            AcademicStandingId: Guid.NewGuid(),
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SingleValidator_ConditionalPromotion_MissingConditions_ShouldFail(string? conditions)
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "conditional_promotion",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: conditions,
            SupportingEvidence: "Conditional standing due to 1 failed unit in Mathematics.",
            AcademicStandingId: Guid.NewGuid(),
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Conditions)
            .WithErrorMessage("Conditions are mandatory when recommendation is 'conditional_promotion'.");
    }

    [Fact]
    public void SingleValidator_ValidRetentionRecommended_ShouldPass()
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "retention_recommended",
            FromLevel: "Grade 7",
            RecommendedToLevel: null,
            Conditions: null,
            SupportingEvidence: "Failed 4 learning units and attendance below 65%.",
            AcademicStandingId: Guid.NewGuid(),
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid_recommendation")]
    [InlineData("promoted")]
    public void SingleValidator_InvalidRecommendation_ShouldFail(string? recommendation)
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: recommendation!,
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: "Supporting evidence text.",
            AcademicStandingId: Guid.NewGuid(),
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Recommendation);
    }

    [Fact]
    public void SingleValidator_EmptyStudentId_ShouldFail()
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.Empty,
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: "Passed all units.",
            AcademicStandingId: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StudentId)
            .WithErrorMessage("Student ID is required.");
    }

    [Fact]
    public void SingleValidator_EmptyAcademicPeriodId_ShouldFail()
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.Empty,
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: "Passed all units.",
            AcademicStandingId: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AcademicPeriodId)
            .WithErrorMessage("Academic period ID is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SingleValidator_EmptyFromLevel_ShouldFail(string? fromLevel)
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: fromLevel!,
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: "Passed all units.",
            AcademicStandingId: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromLevel)
            .WithErrorMessage("From level is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SingleValidator_EmptySupportingEvidence_ShouldFail(string? evidence)
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: evidence!,
            AcademicStandingId: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SupportingEvidence)
            .WithErrorMessage("Supporting evidence is required.");
    }

    [Fact]
    public void SingleValidator_ValidException_WithReasonAndDecree_ShouldPass()
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: "Exceptional promotion approved by Academic Council.",
            AcademicStandingId: Guid.NewGuid(),
            IsException: true,
            ExceptionReason: "Granted committee promotion exception due to medical hospitalization.",
            CommitteeReference: "SK-DG/2026/06-01");

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("Abcd")] // < 5 chars
    public void SingleValidator_IsExceptionTrue_InvalidReason_ShouldFail(string? reason)
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: "Passed all units.",
            AcademicStandingId: null,
            IsException: true,
            ExceptionReason: reason,
            CommitteeReference: "SK-DG/2026/06-01");

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("SK")] // < 5 chars
    public void SingleValidator_IsExceptionTrue_InvalidCommitteeReference_ShouldFail(string? committeeRef)
    {
        // Arrange
        var command = new IssuePromotionRecommendationCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 7",
            RecommendedToLevel: "Grade 8",
            Conditions: null,
            SupportingEvidence: "Passed all units.",
            AcademicStandingId: null,
            IsException: true,
            ExceptionReason: "Granted committee promotion exception due to medical hospitalization.",
            CommitteeReference: committeeRef);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    #endregion

    #region Batch BatchIssuePromotionRecommendationsValidator Tests

    [Fact]
    public void BatchValidator_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new BatchIssuePromotionRecommendationsCommand(
            AcademicPeriodId: Guid.NewGuid(),
            Items:
            [
                new ProgressionItemDto(
                    StudentId: Guid.NewGuid(),
                    Recommendation: "promotion_eligible",
                    FromLevel: "Grade 7",
                    RecommendedToLevel: "Grade 8",
                    Conditions: null,
                    SupportingEvidence: "Met all criteria.",
                    AcademicStandingId: null,
                    IsException: false,
                    ExceptionReason: null,
                    CommitteeReference: null)
            ]);

        // Act
        var result = _batchValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void BatchValidator_EmptyAcademicPeriodId_ShouldFail()
    {
        // Arrange
        var command = new BatchIssuePromotionRecommendationsCommand(
            AcademicPeriodId: Guid.Empty,
            Items:
            [
                new ProgressionItemDto(
                    StudentId: Guid.NewGuid(),
                    Recommendation: "promotion_eligible",
                    FromLevel: "Grade 7",
                    RecommendedToLevel: "Grade 8",
                    Conditions: null,
                    SupportingEvidence: "Met all criteria.",
                    AcademicStandingId: null,
                    IsException: false,
                    ExceptionReason: null,
                    CommitteeReference: null)
            ]);

        // Act
        var result = _batchValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AcademicPeriodId)
            .WithErrorMessage("Academic period ID is required.");
    }

    [Fact]
    public void BatchValidator_EmptyItemsList_ShouldFail()
    {
        // Arrange
        var command = new BatchIssuePromotionRecommendationsCommand(
            AcademicPeriodId: Guid.NewGuid(),
            Items: []);

        // Act
        var result = _batchValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("At least one progression recommendation item is required.");
    }

    #endregion

    #region Item ProgressionItemDtoValidator Tests

    [Fact]
    public void ItemValidator_ValidProgressionItem_ShouldPass()
    {
        // Arrange
        var item = new ProgressionItemDto(
            StudentId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 10",
            RecommendedToLevel: "Grade 11",
            Conditions: null,
            SupportingEvidence: "All high school credits achieved.",
            AcademicStandingId: Guid.NewGuid(),
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _itemValidator.TestValidate(item);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ItemValidator_ConditionalPromotion_MissingConditions_ShouldFail(string? conditions)
    {
        // Arrange
        var item = new ProgressionItemDto(
            StudentId: Guid.NewGuid(),
            Recommendation: "conditional_promotion",
            FromLevel: "Grade 10",
            RecommendedToLevel: "Grade 11",
            Conditions: conditions,
            SupportingEvidence: "Remediation required in English.",
            AcademicStandingId: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _itemValidator.TestValidate(item);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Conditions)
            .WithErrorMessage("Conditions are mandatory when recommendation is 'conditional_promotion'.");
    }

    [Fact]
    public void ItemValidator_IsExceptionTrue_MissingReason_ShouldFail()
    {
        // Arrange
        var item = new ProgressionItemDto(
            StudentId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 10",
            RecommendedToLevel: "Grade 11",
            Conditions: null,
            SupportingEvidence: "Exceptional promotion.",
            AcademicStandingId: null,
            IsException: true,
            ExceptionReason: "",
            CommitteeReference: "SK-DG/2026/06-01");

        // Act
        var result = _itemValidator.TestValidate(item);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
    }

    [Fact]
    public void ItemValidator_IsExceptionTrue_MissingCommitteeReference_ShouldFail()
    {
        // Arrange
        var item = new ProgressionItemDto(
            StudentId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 10",
            RecommendedToLevel: "Grade 11",
            Conditions: null,
            SupportingEvidence: "Exceptional promotion.",
            AcademicStandingId: null,
            IsException: true,
            ExceptionReason: "Granted exception following council review.",
            CommitteeReference: "");

        // Act
        var result = _itemValidator.TestValidate(item);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    #endregion
}
