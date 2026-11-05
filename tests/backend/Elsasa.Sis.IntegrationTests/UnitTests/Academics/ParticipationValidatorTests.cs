namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.AcademicParticipations.BatchRegisterCohortParticipation;
using Elsasa.Sis.Academics.Features.AcademicParticipations.ConcludeParticipation;
using Elsasa.Sis.Academics.Features.AcademicParticipations.RecognizePriorCredit;
using Elsasa.Sis.Academics.Features.AcademicParticipations.RegisterParticipation;
using Elsasa.Sis.Academics.Features.AcademicParticipations.UpdateParticipationStatus;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Academic Participation validators (P6-S14, P2-S04, P2-S05, TD-02).
/// Validates BatchRegisterCohortParticipationValidator, RegisterParticipationValidator,
/// ConcludeParticipationValidator, RecognizePriorCreditValidator, and UpdateParticipationStatusValidator.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class ParticipationValidatorTests
{
    private readonly BatchRegisterCohortParticipationValidator _batchValidator = new();
    private readonly RegisterParticipationValidator _registerValidator = new();
    private readonly ConcludeParticipationValidator _concludeValidator = new();
    private readonly RecognizePriorCreditValidator _recognizeValidator = new();
    private readonly UpdateParticipationStatusValidator _updateStatusValidator = new();

    #region BatchRegisterCohortParticipationValidator Tests

    [Fact]
    public void BatchValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [Guid.NewGuid(), Guid.NewGuid()],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 7, 15),
            StudentIds: [Guid.NewGuid(), Guid.NewGuid()]);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void BatchValidator_Should_Succeed_When_StudentIdsIsNull()
    {
        // Arrange - null studentIds targets all actively enrolled students in the cohort
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [Guid.NewGuid()],
            SectionLabel: "10-B",
            StartDate: new DateOnly(2026, 7, 15),
            StudentIds: null);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_AcademicPeriodIdIsEmpty()
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.Empty,
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [Guid.NewGuid()],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchRegisterCohortParticipationRequest.AcademicPeriodId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BatchValidator_Should_Fail_When_AcademicPeriodLabelIsMissing(string? label)
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: label!,
            LearningUnitIds: [Guid.NewGuid()],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchRegisterCohortParticipationRequest.AcademicPeriodLabel));
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_AcademicPeriodLabelExceeds100Characters()
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: new string('P', 101),
            LearningUnitIds: [Guid.NewGuid()],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchRegisterCohortParticipationRequest.AcademicPeriodLabel)
            && e.ErrorMessage.Contains("cannot exceed 100 characters"));
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_LearningUnitIdsIsEmpty()
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchRegisterCohortParticipationRequest.LearningUnitIds));
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_LearningUnitIdsContainsEmptyGuid()
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [Guid.NewGuid(), Guid.Empty],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith(nameof(BatchRegisterCohortParticipationRequest.LearningUnitIds)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BatchValidator_Should_Fail_When_SectionLabelIsMissing(string? section)
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [Guid.NewGuid()],
            SectionLabel: section!,
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchRegisterCohortParticipationRequest.SectionLabel));
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_SectionLabelExceeds50Characters()
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [Guid.NewGuid()],
            SectionLabel: new string('S', 51),
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchRegisterCohortParticipationRequest.SectionLabel)
            && e.ErrorMessage.Contains("cannot exceed 50 characters"));
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_StartDateIsDefault()
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [Guid.NewGuid()],
            SectionLabel: "10-A",
            StartDate: default);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BatchRegisterCohortParticipationRequest.StartDate));
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_StudentIdsContainsEmptyGuid()
    {
        // Arrange
        var request = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            LearningUnitIds: [Guid.NewGuid()],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 7, 15),
            StudentIds: [Guid.NewGuid(), Guid.Empty]);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith(nameof(BatchRegisterCohortParticipationRequest.StudentIds)));
    }

    #endregion

    #region RegisterParticipationValidator Tests

    [Fact]
    public void RegisterValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            SectionLabel: "10-A",
            ParticipationType: "standard",
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("repeat")]
    [InlineData("transferred_in")]
    [InlineData("recognized_prior")]
    [InlineData("STANDARD")]
    [InlineData("Repeat")]
    public void RegisterValidator_Should_Succeed_For_AllValidParticipationTypes(string participationType)
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            SectionLabel: "10-A",
            ParticipationType: participationType,
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("assigned")]
    [InlineData("student_selected")]
    [InlineData("learning_plan")]
    [InlineData("ASSIGNED")]
    [InlineData("Student_Selected")]
    public void RegisterValidator_Should_Succeed_For_AllValidSources(string source)
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            SectionLabel: null,
            ParticipationType: "standard",
            Source: source,
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterValidator_Should_Fail_When_StudentIdIsEmpty()
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.Empty,
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: "standard",
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.StudentId));
    }

    [Fact]
    public void RegisterValidator_Should_Fail_When_LearningUnitIdIsEmpty()
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.Empty,
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: "standard",
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.LearningUnitId));
    }

    [Fact]
    public void RegisterValidator_Should_Fail_When_AcademicPeriodIdIsEmpty()
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.Empty,
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: "standard",
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.AcademicPeriodId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RegisterValidator_Should_Fail_When_AcademicPeriodLabelIsMissing(string? label)
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: label!,
            ParticipationType: "standard",
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.AcademicPeriodLabel));
    }

    [Fact]
    public void RegisterValidator_Should_Fail_When_AcademicPeriodLabelExceeds100Characters()
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: new string('P', 101),
            ParticipationType: "standard",
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.AcademicPeriodLabel));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("unknown_type")]
    [InlineData("audit")]
    [InlineData("withdrawn")]
    public void RegisterValidator_Should_Fail_When_ParticipationTypeIsInvalid(string? invalidType)
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: invalidType!,
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.ParticipationType));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("automatic")]
    [InlineData("admin")]
    public void RegisterValidator_Should_Fail_When_SourceIsInvalid(string? invalidSource)
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: "standard",
            Source: invalidSource!,
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.Source));
    }

    [Fact]
    public void RegisterValidator_Should_Fail_When_StartDateIsDefault()
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: "standard",
            Source: "assigned",
            StartDate: default);

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.StartDate));
    }

    [Fact]
    public void RegisterValidator_Should_Fail_When_SectionLabelExceeds50Characters()
    {
        // Arrange
        var request = new RegisterParticipationRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            SectionLabel: new string('S', 51),
            ParticipationType: "standard",
            Source: "assigned",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _registerValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterParticipationRequest.SectionLabel));
    }

    #endregion

    #region ConcludeParticipationValidator Tests

    [Fact]
    public void ConcludeValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new ConcludeParticipationRequest(
            CompletionDate: new DateOnly(2026, 12, 20),
            Notes: "Completed all curriculum requirements.");

        // Act
        var result = _concludeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ConcludeValidator_Should_Fail_When_CompletionDateIsDefault()
    {
        // Arrange
        var request = new ConcludeParticipationRequest(CompletionDate: default);

        // Act
        var result = _concludeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConcludeParticipationRequest.CompletionDate));
    }

    [Fact]
    public void ConcludeValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var request = new ConcludeParticipationRequest(
            CompletionDate: new DateOnly(2026, 12, 20),
            Notes: new string('N', 4001));

        // Act
        var result = _concludeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConcludeParticipationRequest.Notes));
    }

    #endregion

    #region RecognizePriorCreditValidator Tests

    [Fact]
    public void RecognizeValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new RecognizePriorCreditRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: "transferred_in",
            SourceInstitution: "Jakarta International School",
            ExternalGradeOrCredit: "A (4.0)",
            RecognitionBasis: "Official transcript evaluated by academic board.",
            StartDate: new DateOnly(2026, 1, 15),
            CompletionDate: new DateOnly(2026, 6, 30),
            Notes: "Credit transferred.");

        // Act
        var result = _recognizeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("transferred_in")]
    [InlineData("recognized_prior")]
    [InlineData("TRANSFERRED_IN")]
    [InlineData("Recognized_Prior")]
    public void RecognizeValidator_Should_Succeed_For_ValidTypes(string type)
    {
        // Arrange
        var request = new RecognizePriorCreditRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: type,
            StartDate: new DateOnly(2026, 1, 15),
            CompletionDate: new DateOnly(2026, 6, 30));

        // Act
        var result = _recognizeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("standard")]
    [InlineData("repeat")]
    [InlineData("invalid")]
    public void RecognizeValidator_Should_Fail_When_TypeIsInvalid(string invalidType)
    {
        // Arrange
        var request = new RecognizePriorCreditRequest(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: invalidType);

        // Act
        var result = _recognizeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecognizePriorCreditRequest.ParticipationType));
    }

    [Fact]
    public void RecognizeValidator_Should_Fail_When_StudentIdIsEmpty()
    {
        // Arrange
        var request = new RecognizePriorCreditRequest(
            StudentId: Guid.Empty,
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Semester 1",
            ParticipationType: "transferred_in");

        // Act
        var result = _recognizeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecognizePriorCreditRequest.StudentId));
    }

    #endregion

    #region UpdateParticipationStatusValidator Tests

    [Theory]
    [InlineData("in_progress")]
    [InlineData("paused")]
    [InlineData("withdrawn")]
    [InlineData("IN_PROGRESS")]
    [InlineData("Paused")]
    public void UpdateStatusValidator_Should_Succeed_For_ValidStatuses(string status)
    {
        // Arrange
        var request = new UpdateParticipationStatusRequest(
            NewStatus: status,
            Notes: "Status change authorized.");

        // Act
        var result = _updateStatusValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("completed")]
    [InlineData("cancelled")]
    [InlineData("active")]
    public void UpdateStatusValidator_Should_Fail_For_InvalidStatuses(string? status)
    {
        // Arrange
        var request = new UpdateParticipationStatusRequest(
            NewStatus: status!,
            Notes: "Invalid status test.");

        // Act
        var result = _updateStatusValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateParticipationStatusRequest.NewStatus));
    }

    [Fact]
    public void UpdateStatusValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var request = new UpdateParticipationStatusRequest(
            NewStatus: "paused",
            Notes: new string('Z', 4001));

        // Act
        var result = _updateStatusValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateParticipationStatusRequest.Notes));
    }

    #endregion
}
