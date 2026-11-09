namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;
using Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;
using Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Academic Result feature validators (<see cref="EvaluateStudentLearningValidator"/>,
/// <see cref="ApproveSummativeResultsValidator"/>, <see cref="PublishAcademicResultsValidator"/>)
/// verifying candidate XOR exclusivity, evaluation model enumeration, scoring thresholds, and batch ID checks (UC-ACD-06, P4-S08, P7-S16).
/// </summary>
[Trait("Category", "UnitTest")]
[Trait("Category", "Unit")]
public sealed class AcademicResultValidatorTests
{
    private readonly EvaluateStudentLearningValidator _evaluateValidator = new();
    private readonly ApproveSummativeResultsValidator _approveValidator = new();
    private readonly PublishAcademicResultsValidator _publishValidator = new();

    #region EvaluateStudentLearningValidator - Candidate Exclusivity (XOR)

    [Fact]
    public void EvaluateValidator_Should_Pass_When_Only_StudentId_Is_Provided()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: 85m,
            ScoreMax: 100m,
            GradeLetter: "B+",
            GradeLabel: "Kompeten",
            NarrativeFeedback: "Great performance.",
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EvaluateValidator_Should_Pass_When_Only_ApplicantId_Is_Provided()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: null,
            ApplicantId: Guid.NewGuid(),
            ScoreNumeric: 90m,
            ScoreMax: 100m,
            GradeLetter: "A",
            GradeLabel: "Sangat Mahir",
            NarrativeFeedback: "Diagnostic intake evaluation.",
            EvaluationModel: "numeric",
            PublishImmediately: true,
            Notes: "Admissions score note");

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_Both_StudentId_And_ApplicantId_Are_Provided()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: Guid.NewGuid(),
            ScoreNumeric: 80m,
            ScoreMax: 100m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("candidate reference (student_id XOR applicant_id)"));
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_Neither_StudentId_Nor_ApplicantId_Is_Provided()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: null,
            ApplicantId: null,
            ScoreNumeric: 80m,
            ScoreMax: 100m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("candidate reference (student_id XOR applicant_id)"));
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_StudentId_Is_Empty_Guid()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.Empty,
            ApplicantId: null,
            ScoreNumeric: 80m,
            ScoreMax: 100m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("StudentId"));
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_ApplicantId_Is_Empty_Guid()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: null,
            ApplicantId: Guid.Empty,
            ScoreNumeric: 80m,
            ScoreMax: 100m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicantId"));
    }

    #endregion

    #region EvaluateStudentLearningValidator - Evaluation Models & Scoring Thresholds

    [Theory]
    [InlineData("numeric")]
    [InlineData("letter_grade")]
    [InlineData("pass_fail")]
    [InlineData("competency")]
    [InlineData("rubric")]
    [InlineData("narrative")]
    [InlineData("completion")]
    public void EvaluateValidator_Should_Pass_For_All_Supported_Evaluation_Models(string model)
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: null,
            ScoreMax: null,
            GradeLetter: "A",
            GradeLabel: "Sangat Mahir",
            NarrativeFeedback: null,
            EvaluationModel: model,
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid_model")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EvaluateValidator_Should_Fail_When_Evaluation_Model_Is_Invalid(string? model)
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: 85m,
            ScoreMax: 100m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: model!,
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.EvaluationModel));
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_ScoreNumeric_Is_Negative()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: -5m,
            ScoreMax: 100m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("negative"));
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_ScoreNumeric_Exceeds_ScoreMax()
    {
        // Arrange: 105 exceeds max of 100
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: 105m,
            ScoreMax: 100m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("score_numeric cannot exceed score_max"));
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_ScoreMax_Is_Zero_Or_Negative()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: 0m,
            ScoreMax: 0m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("score_max must be greater than zero"));
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_GradeLetter_Exceeds_10_Characters()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: 90m,
            ScoreMax: 100m,
            GradeLetter: "VERY_LONG_LETTER_GRADE",
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("grade_letter cannot exceed 10 characters"));
    }

    [Fact]
    public void EvaluateValidator_Should_Fail_When_GradeLabel_Exceeds_100_Characters()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: 90m,
            ScoreMax: 100m,
            GradeLetter: "A",
            GradeLabel: new string('X', 101),
            NarrativeFeedback: null,
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _evaluateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("grade_label cannot exceed 100 characters"));
    }

    #endregion

    #region ApproveSummativeResultsValidator & PublishAcademicResultsValidator

    [Fact]
    public void ApproveValidator_Should_Pass_For_Valid_Result_Ids()
    {
        // Arrange
        var request = new ApproveSummativeResultsRequest([Guid.NewGuid(), Guid.NewGuid()]);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ApproveValidator_Should_Fail_When_ResultIds_Contains_Empty_Guid()
    {
        // Arrange
        var request = new ApproveSummativeResultsRequest([Guid.NewGuid(), Guid.Empty]);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Result ID cannot be empty"));
    }

    [Fact]
    public void PublishValidator_Should_Pass_For_Valid_Result_Ids()
    {
        // Arrange
        var request = new PublishAcademicResultsRequest([Guid.NewGuid()]);

        // Act
        var result = _publishValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PublishValidator_Should_Fail_When_ResultIds_Contains_Empty_Guid()
    {
        // Arrange
        var request = new PublishAcademicResultsRequest([Guid.Empty]);

        // Act
        var result = _publishValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Result ID cannot be empty"));
    }

    #endregion
}
