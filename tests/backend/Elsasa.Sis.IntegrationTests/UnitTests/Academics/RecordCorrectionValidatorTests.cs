namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CorrectApprovedResultValidator"/> verifying mandatory reason length (min 10 characters),
/// correction presence requirement, and score numeric threshold guards (UC-ACD-19, TD-06, P6-S14, P7-S16).
/// </summary>
[Trait("Category", "UnitTest")]
[Trait("Category", "Unit")]
public sealed class RecordCorrectionValidatorTests
{
    private readonly CorrectApprovedResultValidator _validator = new();

    #region Mandatory Reason Length (Min 10 Characters)

    [Theory]
    [InlineData("Koreksi nilai tugas harian karena salah input", true)]
    [InlineData("1234567890", true)]
    [InlineData("123456789", false)]
    [InlineData("Typo", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void Validator_Should_Enforce_Reason_Minimum_10_Characters(string? reason, bool expectedValid)
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 85m,
            Reason: reason!);

        // Act
        var result = _validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
        else
        {
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Reason));
        }
    }

    [Fact]
    public void Validator_Should_Fail_When_Reason_Exceeds_2000_Characters()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 85m,
            Reason: new string('R', 2001));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Reason));
    }

    #endregion

    #region At Least One Altered Field Requirement

    [Fact]
    public void Validator_Should_Fail_When_No_Correction_Fields_Are_Provided()
    {
        // Arrange: Reason is valid, but no field is altered
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: null,
            ScoreMax: null,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            Reason: "Valid reason for correction that is long enough.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("At least one corrected value field"));
    }

    [Fact]
    public void Validator_Should_Pass_When_Only_Narrative_Feedback_Is_Altered()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            NarrativeFeedback: "Updated teacher feedback reflecting revised rubric criteria.",
            Reason: "Revised feedback requested by subject coordinator.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_Should_Pass_When_Only_GradeLetter_Is_Altered()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            GradeLetter: "A-",
            Reason: "Adjusted letter grade post moderation committee review.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Score Threshold & Bound Guards

    [Fact]
    public void Validator_Should_Fail_When_Corrected_ScoreNumeric_Is_Negative()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: -1m,
            Reason: "Adjustment to negative score.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("greater than or equal to 0"));
    }

    [Fact]
    public void Validator_Should_Fail_When_Corrected_ScoreNumeric_Exceeds_ScoreMax()
    {
        // Arrange: 105 exceeds max 100
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 105m,
            ScoreMax: 100m,
            Reason: "Attempted bonus score beyond maximum.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot exceed corrected maximum score"));
    }

    [Fact]
    public void Validator_Should_Fail_When_Corrected_ScoreMax_Is_Zero_Or_Negative()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreMax: 0m,
            Reason: "Setting score max to zero.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("must be greater than 0"));
    }

    [Fact]
    public void Validator_Should_Fail_When_Corrected_GradeLetter_Exceeds_10_Characters()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            GradeLetter: "GRADE_LETTER_TOO_LONG",
            Reason: "Valid reason for correction.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot exceed 10 characters") || e.ErrorMessage.Contains("must not exceed 10 characters"));
    }

    [Fact]
    public void Validator_Should_Fail_When_Evidence_Exceeds_2000_Characters()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 80m,
            Reason: "Valid correction reason.",
            Evidence: new string('E', 2001));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("must not exceed 2000 characters"));
    }

    #endregion
}
