namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.Assessments.CancelAssessment;
using Elsasa.Sis.Academics.Features.Assessments.ConductAssessment;
using Elsasa.Sis.Academics.Features.Assessments.CreateAssessment;
using Elsasa.Sis.Academics.Features.Assessments.UpdateAssessment;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Assessment feature validators (P2-S04, UC-ACD-05, ACD-WF-07).
/// </summary>
[Trait("Category", "UnitTest")]
[Trait("Category", "Unit")]
public sealed class AssessmentValidatorTests
{
    private readonly CreateAssessmentValidator _createValidator = new();
    private readonly UpdateAssessmentValidator _updateValidator = new();
    private readonly ConductAssessmentValidator _conductValidator = new();
    private readonly CancelAssessmentValidator _cancelValidator = new();

    #region CreateAssessmentValidator Tests

    [Fact]
    public void CreateValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: Guid.NewGuid(),
            Title: "Mid-Term Written Examination",
            AssessmentType: "summative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 75m,
            ReassessmentPolicy: "highest_score",
            Criteria: "Rubric Criteria Standard",
            AssessmentDate: new DateOnly(2026, 11, 20),
            AssessorReference: "Teacher Jane",
            Notes: "Bring pen and calculator.");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_OptionalFieldsAreNull()
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: null,
            Title: "Quick Diagnostic Check",
            AssessmentType: "diagnostic",
            AssessmentMethod: "observation",
            MaxScore: null,
            PassingScore: null,
            ReassessmentPolicy: null,
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_LearningUnitIdIsEmpty()
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.Empty,
            LearningActivityId: null,
            Title: "Invalid Unit Assessment",
            AssessmentType: "formative",
            AssessmentMethod: "oral",
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: "highest_score",
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.LearningUnitId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateValidator_Should_Fail_When_TitleIsMissing(string? title)
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: null,
            Title: title!,
            AssessmentType: "formative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: "highest_score",
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Title));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_TitleExceeds200Characters()
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: null,
            Title: new string('X', 201),
            AssessmentType: "formative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: "highest_score",
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Title));
    }

    [Theory]
    [InlineData("invalid_type")]
    [InlineData("random")]
    [InlineData("")]
    public void CreateValidator_Should_Fail_When_AssessmentTypeIsInvalid(string invalidType)
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: null,
            Title: "Type Test",
            AssessmentType: invalidType,
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: "highest_score",
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.AssessmentType));
    }

    [Theory]
    [InlineData("invalid_method")]
    [InlineData("unsupported")]
    public void CreateValidator_Should_Fail_When_AssessmentMethodIsInvalid(string invalidMethod)
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: null,
            Title: "Method Test",
            AssessmentType: "formative",
            AssessmentMethod: invalidMethod,
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: "highest_score",
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.AssessmentMethod));
    }

    [Theory]
    [InlineData("invalid_policy")]
    [InlineData("custom")]
    public void CreateValidator_Should_Fail_When_ReassessmentPolicyIsInvalid(string invalidPolicy)
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: null,
            Title: "Policy Test",
            AssessmentType: "formative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: invalidPolicy,
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ReassessmentPolicy));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_PassingScoreExceedsMaxScore()
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: null,
            Title: "Score Inconsistency",
            AssessmentType: "summative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 105m,
            ReassessmentPolicy: "highest_score",
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.PassingScore) && e.ErrorMessage.Contains("less than or equal to"));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_ScoresAreNegative()
    {
        // Arrange
        var request = new CreateAssessmentRequest(
            LearningUnitId: Guid.NewGuid(),
            LearningActivityId: null,
            Title: "Negative Score",
            AssessmentType: "summative",
            AssessmentMethod: "written_test",
            MaxScore: -10m,
            PassingScore: -5m,
            ReassessmentPolicy: "highest_score",
            Criteria: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.MaxScore));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.PassingScore));
    }

    #endregion

    #region UpdateAssessmentValidator Tests

    [Fact]
    public void UpdateValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new UpdateAssessmentRequest(
            Title: "Updated Assessment Title",
            Criteria: "Updated Rubrics",
            MaxScore: 100m,
            PassingScore: 80m,
            ReassessmentPolicy: "capped_at_passing",
            AssessmentDate: new DateOnly(2026, 11, 25),
            AssessorReference: "Lead Teacher",
            Notes: "Updated instructions.");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void UpdateValidator_Should_Fail_When_TitleIsMissing(string? title)
    {
        // Arrange
        var request = new UpdateAssessmentRequest(
            Title: title!,
            Criteria: null,
            MaxScore: 100m,
            PassingScore: 70m,
            ReassessmentPolicy: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Title));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_PassingScoreExceedsMaxScore()
    {
        // Arrange
        var request = new UpdateAssessmentRequest(
            Title: "Updated Assessment",
            Criteria: null,
            MaxScore: 50m,
            PassingScore: 55m,
            ReassessmentPolicy: null,
            AssessmentDate: null,
            AssessorReference: null,
            Notes: null);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.PassingScore));
    }

    #endregion

    #region ConductAssessmentValidator Tests

    [Fact]
    public void ConductValidator_Should_Succeed_When_RequestIsEmpty()
    {
        // Arrange
        var request = new ConductAssessmentRequest();

        // Act
        var result = _conductValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConductValidator_Should_Succeed_When_NotesProvided()
    {
        // Arrange
        var request = new ConductAssessmentRequest(
            AssessmentDate: new DateOnly(2026, 11, 20),
            Notes: "All candidates attended on time.");

        // Act
        var result = _conductValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConductValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var request = new ConductAssessmentRequest(
            AssessmentDate: null,
            Notes: new string('N', 4001));

        // Act
        var result = _conductValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Notes));
    }

    #endregion

    #region CancelAssessmentValidator Tests

    [Fact]
    public void CancelValidator_Should_Succeed_When_ReasonIsValid()
    {
        // Arrange
        var request = new CancelAssessmentRequest(Reason: "Rescheduled due to school excursion.");

        // Act
        var result = _cancelValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CancelValidator_Should_Fail_When_ReasonIsEmpty(string? reason)
    {
        // Arrange
        var request = new CancelAssessmentRequest(Reason: reason!);

        // Act
        var result = _cancelValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Reason));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("abcd")]
    public void CancelValidator_Should_Fail_When_ReasonIsLessThan5Characters(string shortReason)
    {
        // Arrange
        var request = new CancelAssessmentRequest(Reason: shortReason);

        // Act
        var result = _cancelValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Reason) && e.ErrorMessage.Contains("at least 5"));
    }

    [Fact]
    public void CancelValidator_Should_Fail_When_ReasonExceeds2000Characters()
    {
        // Arrange
        var request = new CancelAssessmentRequest(Reason: new string('R', 2001));

        // Act
        var result = _cancelValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Reason));
    }

    #endregion
}
