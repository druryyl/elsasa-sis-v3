namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.AddAssessmentEvidence;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class AssessmentEvidenceValidatorTests
{
    private readonly AddAssessmentEvidenceValidator _addValidator = new();
    private readonly UpdateAssessmentEvidenceValidator _updateValidator = new();

    #region AddAssessmentEvidenceValidator Tests

    [Fact]
    public void AddValidator_Should_Pass_When_PayloadIsValid()
    {
        // Arrange
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: "reading_readiness",
            Score: "85/100",
            Source: "External Assessment Center",
            AssessmentDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            Notes: "Student performed well on reading comprehension.");

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void AddValidator_Should_Fail_When_AssessmentTypeIsNullOrEmpty(string? assessmentType)
    {
        // Arrange
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: assessmentType!,
            Score: "80",
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AssessmentType)
            .WithErrorMessage("Assessment type is required.");
    }

    [Fact]
    public void AddValidator_Should_Fail_When_AssessmentTypeExceeds100Chars()
    {
        // Arrange
        var longType = new string('a', 101);
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: longType,
            Score: "80",
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AssessmentType)
            .WithErrorMessage("Assessment type cannot exceed 100 characters.");
    }

    [Fact]
    public void AddValidator_Should_Pass_When_AssessmentTypeIsExactly100Chars()
    {
        // Arrange
        var exactType = new string('a', 100);
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: exactType,
            Score: "80",
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AssessmentType);
    }

    [Fact]
    public void AddValidator_Should_Fail_When_ScoreExceeds50Chars()
    {
        // Arrange
        var longScore = new string('x', 51);
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: "math_readiness",
            Score: longScore,
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Score)
            .WithErrorMessage("Score cannot exceed 50 characters.");
    }

    [Theory]
    [InlineData("85/100")]
    [InlineData("Passed")]
    [InlineData("Grade A")]
    [InlineData("Band 4")]
    [InlineData(null)]
    [InlineData("")]
    public void AddValidator_Should_Pass_When_ScoreIsWithin50Chars(string? score)
    {
        // Arrange
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: "diagnostic_test",
            Score: score,
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Score);
    }

    [Fact]
    public void AddValidator_Should_Fail_When_AssessmentDateIsInFuture()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: "math_readiness",
            Score: "Passed",
            Source: "Center",
            AssessmentDate: futureDate,
            Notes: null);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AssessmentDate)
            .WithErrorMessage("Assessment date cannot be in the future.");
    }

    [Fact]
    public void AddValidator_Should_Pass_When_AssessmentDateIsTodayOrPast()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var past = today.AddDays(-30);

        var requestToday = new AddAssessmentEvidenceRequest(
            AssessmentType: "math_readiness",
            Score: "Passed",
            Source: "Center",
            AssessmentDate: today,
            Notes: null);

        var requestPast = new AddAssessmentEvidenceRequest(
            AssessmentType: "math_readiness",
            Score: "Passed",
            Source: "Center",
            AssessmentDate: past,
            Notes: null);

        // Act & Assert
        _addValidator.TestValidate(requestToday).ShouldNotHaveValidationErrorFor(x => x.AssessmentDate);
        _addValidator.TestValidate(requestPast).ShouldNotHaveValidationErrorFor(x => x.AssessmentDate);
    }

    #endregion

    #region UpdateAssessmentEvidenceValidator Tests

    [Fact]
    public void UpdateValidator_Should_Pass_When_PayloadIsValid()
    {
        // Arrange
        var request = new UpdateAssessmentEvidenceRequest(
            AssessmentType: "psychological_evaluation",
            Score: "Band 4",
            Source: "Child Psychology Clinic",
            AssessmentDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            Notes: "Verified diagnostic observation.");

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void UpdateValidator_Should_Fail_When_AssessmentTypeIsNullOrEmpty(string? assessmentType)
    {
        // Arrange
        var request = new UpdateAssessmentEvidenceRequest(
            AssessmentType: assessmentType!,
            Score: "80",
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AssessmentType)
            .WithErrorMessage("Assessment type is required.");
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_AssessmentTypeExceeds100Chars()
    {
        // Arrange
        var longType = new string('x', 101);
        var request = new UpdateAssessmentEvidenceRequest(
            AssessmentType: longType,
            Score: "80",
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AssessmentType)
            .WithErrorMessage("Assessment type cannot exceed 100 characters.");
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_ScoreExceeds50Chars()
    {
        // Arrange
        var longScore = new string('z', 51);
        var request = new UpdateAssessmentEvidenceRequest(
            AssessmentType: "math_readiness",
            Score: longScore,
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Score)
            .WithErrorMessage("Score cannot exceed 50 characters.");
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_AssessmentDateIsInFuture()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var request = new UpdateAssessmentEvidenceRequest(
            AssessmentType: "math_readiness",
            Score: "Passed",
            Source: "Center",
            AssessmentDate: futureDate,
            Notes: null);

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AssessmentDate)
            .WithErrorMessage("Assessment date cannot be in the future.");
    }

    #endregion
}
