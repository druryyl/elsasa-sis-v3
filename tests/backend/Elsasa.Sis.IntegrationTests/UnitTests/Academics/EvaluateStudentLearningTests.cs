namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="EvaluateStudentLearningValidator"/> (UC-ACD-06, P4-S08, TD-03, TD-04).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class EvaluateStudentLearningTests
{
    private readonly EvaluateStudentLearningValidator _validator = new();

    [Fact]
    public void Validator_Should_Succeed_When_StudentEvaluationIsValid()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: 88.5m,
            ScoreMax: 100m,
            GradeLetter: "A",
            GradeLabel: "Sangat Mahir",
            NarrativeFeedback: "Outstanding analytical thinking in problem solving.",
            EvaluationModel: "numeric",
            PublishImmediately: false,
            Notes: "Mid-term formative quiz.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validator_Should_Succeed_When_ApplicantEntranceEvaluationIsValid()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: null,
            ApplicantId: Guid.NewGuid(),
            ScoreNumeric: 78m,
            ScoreMax: 100m,
            GradeLetter: "B+",
            GradeLabel: "Kompeten",
            NarrativeFeedback: "Candidate demonstrated solid grasp of algebra.",
            EvaluationModel: "numeric",
            PublishImmediately: true,
            Notes: "Admissions entrance diagnostic exam.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("numeric")]
    [InlineData("letter_grade")]
    [InlineData("pass_fail")]
    [InlineData("competency")]
    [InlineData("rubric")]
    [InlineData("narrative")]
    [InlineData("completion")]
    [InlineData("NUMERIC")]
    [InlineData(" Letter_Grade ")]
    public void Validator_Should_Succeed_For_All_Valid_Evaluation_Models(string model)
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: null,
            ScoreMax: null,
            GradeLetter: "A",
            GradeLabel: "Kompeten",
            NarrativeFeedback: "Great work",
            EvaluationModel: model,
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_Should_Fail_When_Both_StudentId_And_ApplicantId_Are_Provided()
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
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Exactly one candidate reference"));
    }

    [Fact]
    public void Validator_Should_Fail_When_Neither_StudentId_Nor_ApplicantId_Is_Provided()
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
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Exactly one candidate reference"));
    }

    [Fact]
    public void Validator_Should_Fail_When_StudentId_Is_Empty_Guid()
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
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("StudentId"));
    }

    [Fact]
    public void Validator_Should_Fail_When_ApplicantId_Is_Empty_Guid()
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
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicantId"));
    }

    [Fact]
    public void Validator_Should_Fail_When_EvaluationModel_Is_Invalid()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: 80m,
            ScoreMax: 100m,
            GradeLetter: null,
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "invalid_model",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EvaluationModel");
    }

    [Fact]
    public void Validator_Should_Fail_When_ScoreNumeric_Is_Negative()
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
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ScoreNumeric"));
    }

    [Fact]
    public void Validator_Should_Fail_When_ScoreNumeric_Exceeds_ScoreMax()
    {
        // Arrange
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
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ScoreNumeric"));
    }

    [Fact]
    public void Validator_Should_Fail_When_GradeLetter_Exceeds_MaxLength()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: null,
            ScoreMax: null,
            GradeLetter: new string('A', 11),
            GradeLabel: null,
            NarrativeFeedback: null,
            EvaluationModel: "letter_grade",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GradeLetter");
    }

    [Fact]
    public void Validator_Should_Fail_When_GradeLabel_Exceeds_MaxLength()
    {
        // Arrange
        var request = new EvaluateStudentLearningRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            ScoreNumeric: null,
            ScoreMax: null,
            GradeLetter: null,
            GradeLabel: new string('L', 101),
            NarrativeFeedback: null,
            EvaluationModel: "competency",
            PublishImmediately: false,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GradeLabel");
    }
}
