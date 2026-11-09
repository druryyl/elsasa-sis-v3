namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectPromotionRecommendation;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

/// <summary>
/// Unit tests for auditable record corrections of academic standings and promotion recommendations (P5-S12, UC-ACD-15, TD-05).
/// Verifies validators, domain constraints, exception fields, reason length, and event contracts.
/// </summary>
[Trait("Category", "UnitTest")]
[Trait("Category", "Unit")]
public sealed class StandingAndProgressionCorrectionTests
{
    private readonly CorrectAcademicStandingRequestValidator _standingRequestValidator = new();
    private readonly CorrectAcademicStandingCommandValidator _standingCommandValidator = new();
    private readonly CorrectPromotionRecommendationRequestValidator _progressionRequestValidator = new();
    private readonly CorrectPromotionRecommendationCommandValidator _progressionCommandValidator = new();

    #region 1. CorrectAcademicStanding Validation Tests

    [Theory]
    [InlineData("good_standing")]
    [InlineData("conditional_standing")]
    [InlineData("not_in_good_standing")]
    public void StandingRequestValidator_Should_Pass_For_Valid_Standings(string standing)
    {
        // Arrange
        var request = new CorrectAcademicStandingRequest(
            Standing: standing,
            PolicyReference: "POL-ACD-2026-01",
            Conditions: standing == "conditional_standing" ? "Must complete math remediation" : null,
            Reason: "Administrative correction approved by Academic Committee");

        // Act
        var result = _standingRequestValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Standing);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
        result.ShouldNotHaveValidationErrorFor(x => x.PolicyReference);
    }

    [Theory]
    [InlineData("invalid_standing")]
    [InlineData("passed")]
    [InlineData("")]
    public void StandingRequestValidator_Should_Fail_For_Invalid_Standing(string invalidStanding)
    {
        // Arrange
        var request = new CorrectAcademicStandingRequest(
            Standing: invalidStanding,
            PolicyReference: "POL-ACD-2026-01",
            Reason: "Administrative correction approved by Academic Committee");

        // Act
        var result = _standingRequestValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Standing);
    }

    [Theory]
    [InlineData("Too short", false)]
    [InlineData("123456789", false)]
    [InlineData("", false)]
    [InlineData("1234567890", true)]
    [InlineData("Standing corrected following grade appeals board decree", true)]
    public void StandingRequestValidator_Should_Enforce_Reason_Minimum_10_Chars(string reason, bool expectedValid)
    {
        // Arrange
        var request = new CorrectAcademicStandingRequest(
            Standing: "good_standing",
            PolicyReference: "POL-ACD-2026-01",
            Reason: reason);

        // Act
        var result = _standingRequestValidator.TestValidate(request);

        // Assert
        if (expectedValid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Reason);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }
    }

    [Fact]
    public void StandingRequestValidator_Should_Require_Conditions_When_ConditionalStanding()
    {
        // Arrange
        var request = new CorrectAcademicStandingRequest(
            Standing: "conditional_standing",
            PolicyReference: "POL-ACD-2026-01",
            Conditions: null,
            Reason: "Administrative correction approved by Academic Committee");

        // Act
        var result = _standingRequestValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Conditions);
    }

    [Fact]
    public void StandingRequestValidator_Should_Require_Exception_Fields_When_IsException_True()
    {
        // Arrange
        var request = new CorrectAcademicStandingRequest(
            Standing: "good_standing",
            PolicyReference: "POL-ACD-2026-01",
            IsException: true,
            ExceptionReason: null,
            CommitteeReference: null,
            Reason: "Administrative correction approved by Academic Committee");

        // Act
        var result = _standingRequestValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    [Fact]
    public void StandingRequestValidator_Should_Pass_When_Exception_Fields_Provided()
    {
        // Arrange
        var request = new CorrectAcademicStandingRequest(
            Standing: "good_standing",
            PolicyReference: "POL-ACD-2026-01",
            IsException: true,
            ExceptionReason: "Principal council authorized exception for extenuating circumstances",
            CommitteeReference: "SK-DG/2026/06-01",
            Reason: "Administrative correction approved by Academic Committee");

        // Act
        var result = _standingRequestValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldNotHaveValidationErrorFor(x => x.CommitteeReference);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void StandingCommandValidator_Should_Require_Id()
    {
        // Arrange
        var command = new CorrectAcademicStandingCommand(
            Id: Guid.Empty,
            Standing: "good_standing",
            PolicyReference: "POL-ACD-2026-01",
            Conditions: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null,
            Reason: "Correction authorized by Academic Authority");

        // Act
        var result = _standingCommandValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region 2. CorrectPromotionRecommendation Validation Tests

    [Theory]
    [InlineData("promotion_eligible")]
    [InlineData("retention_recommended")]
    [InlineData("conditional_promotion")]
    public void ProgressionRequestValidator_Should_Pass_For_Valid_Recommendations(string recommendation)
    {
        // Arrange
        var request = new CorrectPromotionRecommendationRequest(
            Recommendation: recommendation,
            FromLevel: "Grade 4",
            RecommendedToLevel: recommendation == "retention_recommended" ? null : "Grade 5",
            Conditions: recommendation == "conditional_promotion" ? "Pass remedial math exam" : null,
            SupportingEvidence: "All unit competencies verified with approved semester results",
            Reason: "Administrative correction approved by Academic Authority");

        // Act
        var result = _progressionRequestValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Recommendation);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
        result.ShouldNotHaveValidationErrorFor(x => x.FromLevel);
        result.ShouldNotHaveValidationErrorFor(x => x.SupportingEvidence);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("promoted")]
    [InlineData("repeat")]
    [InlineData("")]
    public void ProgressionRequestValidator_Should_Fail_For_Invalid_Recommendation(string invalidRecommendation)
    {
        // Arrange
        var request = new CorrectPromotionRecommendationRequest(
            Recommendation: invalidRecommendation,
            FromLevel: "Grade 4",
            SupportingEvidence: "Verified evidence",
            Reason: "Administrative correction approved by Academic Authority");

        // Act
        var result = _progressionRequestValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Recommendation);
    }

    [Theory]
    [InlineData("Too short", false)]
    [InlineData("123456789", false)]
    [InlineData("", false)]
    [InlineData("1234567890", true)]
    [InlineData("Promotion corrected per headmaster decree after summer remediation", true)]
    public void ProgressionRequestValidator_Should_Enforce_Reason_Minimum_10_Chars(string reason, bool expectedValid)
    {
        // Arrange
        var request = new CorrectPromotionRecommendationRequest(
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            SupportingEvidence: "Competency criteria satisfied",
            Reason: reason);

        // Act
        var result = _progressionRequestValidator.TestValidate(request);

        // Assert
        if (expectedValid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Reason);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }
    }

    [Fact]
    public void ProgressionRequestValidator_Should_Require_Conditions_When_ConditionalPromotion()
    {
        // Arrange
        var request = new CorrectPromotionRecommendationRequest(
            Recommendation: "conditional_promotion",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            Conditions: null,
            SupportingEvidence: "Requires supplementary summer course",
            Reason: "Administrative correction approved by Academic Authority");

        // Act
        var result = _progressionRequestValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Conditions);
    }

    [Fact]
    public void ProgressionRequestValidator_Should_Require_Exception_Fields_When_IsException_True()
    {
        // Arrange
        var request = new CorrectPromotionRecommendationRequest(
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            SupportingEvidence: "Standard competencies evaluated",
            IsException: true,
            ExceptionReason: null,
            CommitteeReference: null,
            Reason: "Administrative correction approved by Academic Authority");

        // Act
        var result = _progressionRequestValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    [Fact]
    public void ProgressionCommandValidator_Should_Require_Id()
    {
        // Arrange
        var command = new CorrectPromotionRecommendationCommand(
            Id: Guid.Empty,
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            Conditions: null,
            SupportingEvidence: "Competency met",
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null,
            Reason: "Correction authorized by Academic Authority");

        // Act
        var result = _progressionCommandValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region 3. Domain Event Records Contracts

    [Fact]
    public void AcademicStandingCorrectedEvent_Should_Capture_Expected_Properties()
    {
        // Arrange
        var standingId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var evt = new AcademicStandingCorrectedEvent(
            StandingId: standingId,
            StudentId: studentId,
            AcademicPeriodId: periodId,
            OldStanding: "conditional_standing",
            NewStanding: "good_standing",
            Reason: "Remedial program successfully completed and verified",
            OccurredOn: now);

        // Assert
        evt.StandingId.Should().Be(standingId);
        evt.StudentId.Should().Be(studentId);
        evt.AcademicPeriodId.Should().Be(periodId);
        evt.OldStanding.Should().Be("conditional_standing");
        evt.NewStanding.Should().Be("good_standing");
        evt.Reason.Should().Be("Remedial program successfully completed and verified");
        evt.OccurredOn.Should().Be(now);
    }

    [Fact]
    public void PromotionRecommendationCorrectedEvent_Should_Capture_Expected_Properties()
    {
        // Arrange
        var recommendationId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var evt = new PromotionRecommendationCorrectedEvent(
            RecommendationId: recommendationId,
            StudentId: studentId,
            AcademicPeriodId: periodId,
            OldRecommendation: "retention_recommended",
            NewRecommendation: "conditional_promotion",
            Reason: "Student showed exceptional progress in supplementary learning unit",
            OccurredOn: now);

        // Assert
        evt.RecommendationId.Should().Be(recommendationId);
        evt.StudentId.Should().Be(studentId);
        evt.AcademicPeriodId.Should().Be(periodId);
        evt.OldRecommendation.Should().Be("retention_recommended");
        evt.NewRecommendation.Should().Be("conditional_promotion");
        evt.Reason.Should().Be("Student showed exceptional progress in supplementary learning unit");
        evt.OccurredOn.Should().Be(now);
    }

    #endregion
}
