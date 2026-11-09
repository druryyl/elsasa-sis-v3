namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ArchiveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.RemoveAcademicPlanAdjustment;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.RemoveSlpLearningUnit;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ReviseStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.SubmitStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.SyncSlpParticipations;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Unit tests for Student Learning Plan request validators (P2-S04, P2-S05, P3-S07, P3-S08, P3-S09, UC-ACD-09).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class StudentLearningPlanValidatorsTests
{
    private readonly CreateStudentLearningPlanValidator _createValidator = new();
    private readonly UpdateStudentLearningPlanValidator _updateValidator = new();
    private readonly ArchiveStudentLearningPlanValidator _archiveValidator = new();
    private readonly AddSlpLearningUnitValidator _unitValidator = new();
    private readonly AddAcademicPlanAdjustmentValidator _adjustmentValidator = new();
    private readonly SubmitStudentLearningPlanValidator _submitValidator = new();
    private readonly ApproveStudentLearningPlanValidator _approveValidator = new();
    private readonly ReviseStudentLearningPlanValidator _reviseValidator = new();

    #region CreateStudentLearningPlanValidator Tests

    [Fact]
    public void CreateValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Notes: "Initial draft plan for individualized learning adaptation.");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_StudentIdIsEmpty()
    {
        // Arrange
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.Empty,
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1 TP 2029/2030");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStudentLearningPlanRequest.StudentId)
            && e.ErrorMessage.Contains("Student ID is required"));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_AcademicPeriodIdIsEmpty()
    {
        // Arrange
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.Empty,
            AcademicPeriodLabel: "Semester 1 TP 2029/2030");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStudentLearningPlanRequest.AcademicPeriodId)
            && e.ErrorMessage.Contains("Academic period ID is required"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateValidator_Should_Fail_When_AcademicPeriodLabelIsBlank(string? blankLabel)
    {
        // Arrange
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: blankLabel!);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStudentLearningPlanRequest.AcademicPeriodLabel));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_AcademicPeriodLabelExceeds100Characters()
    {
        // Arrange
        var longLabel = new string('A', 101);
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: longLabel);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStudentLearningPlanRequest.AcademicPeriodLabel)
            && e.ErrorMessage.Contains("cannot exceed 100 characters"));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_NotesExceeds4000Characters()
    {
        // Arrange
        var longNotes = new string('N', 4001);
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Notes: longNotes);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStudentLearningPlanRequest.Notes)
            && e.ErrorMessage.Contains("cannot exceed 4000 characters"));
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_AcademicPeriodLabelIsExactly100Characters()
    {
        // Arrange
        var boundaryLabel = new string('L', 100);
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: boundaryLabel,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_NotesIsNull()
    {
        // Arrange
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_NotesIsEmpty()
    {
        // Arrange
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Notes: string.Empty);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_NotesIsExactly4000Characters()
    {
        // Arrange
        var boundaryNotes = new string('N', 4000);
        var request = new CreateStudentLearningPlanRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Notes: boundaryNotes);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region UpdateStudentLearningPlanValidator Tests

    [Fact]
    public void UpdateValidator_Should_Succeed_When_NotesAreNull()
    {
        // Arrange
        var request = new UpdateStudentLearningPlanRequest(Notes: null);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void UpdateValidator_Should_Succeed_When_NotesAreValid()
    {
        // Arrange
        var request = new UpdateStudentLearningPlanRequest(Notes: "Updated plan notes for review.");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('X', 4001);
        var request = new UpdateStudentLearningPlanRequest(Notes: longNotes);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStudentLearningPlanRequest.Notes)
            && e.ErrorMessage.Contains("cannot exceed 4000 characters"));
    }

    #endregion

    #region ArchiveStudentLearningPlanValidator Tests

    [Theory]
    [InlineData("archived")]
    [InlineData("ARCHIVED")]
    [InlineData("completed")]
    [InlineData("COMPLETED")]
    [InlineData(null)]
    [InlineData("")]
    public void ArchiveValidator_Should_Succeed_When_TargetStatusIsValid(string? status)
    {
        // Arrange
        var request = new ArchiveStudentLearningPlanRequest(TargetStatus: status, Notes: "Archival notes");

        // Act
        var result = _archiveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("active")]
    [InlineData("draft")]
    [InlineData("revised")]
    [InlineData("deleted")]
    [InlineData("unknown")]
    public void ArchiveValidator_Should_Fail_When_TargetStatusIsInvalid(string invalidStatus)
    {
        // Arrange
        var request = new ArchiveStudentLearningPlanRequest(TargetStatus: invalidStatus);

        // Act
        var result = _archiveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ArchiveStudentLearningPlanRequest.TargetStatus)
            && e.ErrorMessage.Contains("must be either 'archived' or 'completed'"));
    }

    [Fact]
    public void ArchiveValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('Z', 4001);
        var request = new ArchiveStudentLearningPlanRequest(TargetStatus: "archived", Notes: longNotes);

        // Act
        var result = _archiveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ArchiveStudentLearningPlanRequest.Notes)
            && e.ErrorMessage.Contains("cannot exceed 4000 characters"));
    }

    #endregion

    #region AddAcademicPlanAdjustmentValidator Tests

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "50% extra time during summative written assessments and quiet testing space.",
            Rationale: "Recommended in verified psycho-educational assessment report to manage processing speed limitations.",
            EffectiveDate: new DateOnly(2029, 9, 1),
            EndDate: new DateOnly(2030, 6, 30),
            Notes: "Reviewed by special education coordinator.");

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("accommodation")]
    [InlineData("substitution")]
    [InlineData("exemption")]
    [InlineData("Accommodation")]
    [InlineData("SUBSTITUTION")]
    [InlineData("Exemption")]
    public void AdjustmentValidator_Should_Succeed_For_AllValidAdjustmentTypes(string adjustmentType)
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: adjustmentType,
            Description: "Valid adjustment description.",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid_type")]
    [InlineData("waiver")]
    public void AdjustmentValidator_Should_Fail_When_AdjustmentTypeIsInvalid(string adjustmentType)
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: adjustmentType,
            Description: "Valid description.",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.AdjustmentType));
    }

    [Fact]
    public void AdjustmentValidator_Should_Fail_When_DescriptionIsEmpty()
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.Description)
            && e.ErrorMessage.Contains("Description is required"));
    }

    [Fact]
    public void AdjustmentValidator_Should_Fail_When_DescriptionExceeds2000Characters()
    {
        // Arrange
        var longDesc = new string('D', 2001);
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: longDesc,
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.Description)
            && e.ErrorMessage.Contains("must not exceed 2000 characters"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AdjustmentValidator_Should_Fail_When_RationaleIsEmpty(string rationale)
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: rationale,
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.Rationale));
    }

    [Fact]
    public void AdjustmentValidator_Should_Fail_When_RationaleIsShorterThan10Characters()
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "Too short",
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.Rationale)
            && e.ErrorMessage.Contains("at least 10 characters"));
    }

    [Fact]
    public void AdjustmentValidator_Should_Fail_When_RationaleExceeds2000Characters()
    {
        // Arrange
        var longRationale = new string('R', 2001);
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: longRationale,
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.Rationale)
            && e.ErrorMessage.Contains("must not exceed 2000 characters"));
    }

    [Fact]
    public void AdjustmentValidator_Should_Fail_When_EffectiveDateIsDefault()
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: default);

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.EffectiveDate));
    }

    [Fact]
    public void AdjustmentValidator_Should_Fail_When_EndDateIsBeforeEffectiveDate()
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1),
            EndDate: new DateOnly(2029, 8, 31));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.EndDate)
            && e.ErrorMessage.Contains("on or after the effective date"));
    }

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_EndDateEqualsEffectiveDate()
    {
        // Arrange
        var sameDate = new DateOnly(2029, 9, 1);
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: sameDate,
            EndDate: sameDate);

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AdjustmentValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('N', 4001);
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1),
            Notes: longNotes);

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.Notes)
            && e.ErrorMessage.Contains("must not exceed 4000 characters"));
    }

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_EndDateIsAfterEffectiveDate()
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "substitution",
            Description: "Adaptive PE instead of standard PE.",
            Rationale: "Pedagogical recommendation for adaptive physical mobility.",
            EffectiveDate: new DateOnly(2029, 9, 1),
            EndDate: new DateOnly(2030, 6, 30));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_EndDateIsNull()
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "exemption",
            Description: "Language exemption based on foreign transfer documentation.",
            Rationale: "Transfer student from non-Indonesian curriculum pathway.",
            EffectiveDate: new DateOnly(2029, 9, 1),
            EndDate: null);

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_RationaleIsExactly10Characters()
    {
        // Arrange (10 characters: "1234567890")
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "1234567890",
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AdjustmentValidator_Should_Fail_When_RationaleIsExactly9Characters()
    {
        // Arrange (9 characters: "123456789")
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "123456789",
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAcademicPlanAdjustmentRequest.Rationale)
            && e.ErrorMessage.Contains("at least 10 characters"));
    }

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_RationaleIsExactly2000Characters()
    {
        // Arrange
        var boundaryRationale = new string('R', 2000);
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: boundaryRationale,
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_DescriptionIsExactly2000Characters()
    {
        // Arrange
        var boundaryDesc = new string('D', 2000);
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: boundaryDesc,
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1));

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_NotesIsNull()
    {
        // Arrange
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1),
            Notes: null);

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AdjustmentValidator_Should_Succeed_When_NotesIsExactly4000Characters()
    {
        // Arrange
        var boundaryNotes = new string('N', 4000);
        var request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "Valid description.",
            Rationale: "Valid rationale of appropriate length.",
            EffectiveDate: new DateOnly(2029, 9, 1),
            Notes: boundaryNotes);

        // Act
        var result = _adjustmentValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region AddSlpLearningUnitValidator Tests

    [Fact]
    public void UnitValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new AddSlpLearningUnitRequest(
            LearningUnitId: Guid.NewGuid(),
            SequenceOrder: 1,
            IsRequired: true,
            Notes: "Remedial Math at Grade 3 pacing.");

        // Act
        var result = _unitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void UnitValidator_Should_Succeed_When_SequenceOrderAndNotesAreNull()
    {
        // Arrange
        var request = new AddSlpLearningUnitRequest(
            LearningUnitId: Guid.NewGuid(),
            SequenceOrder: null,
            IsRequired: false,
            Notes: null);

        // Act
        var result = _unitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void UnitValidator_Should_Fail_When_LearningUnitIdIsEmpty()
    {
        // Arrange
        var request = new AddSlpLearningUnitRequest(
            LearningUnitId: Guid.Empty,
            SequenceOrder: 1);

        // Act
        var result = _unitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddSlpLearningUnitRequest.LearningUnitId)
            && e.ErrorMessage.Contains("Learning unit ID is required"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void UnitValidator_Should_Fail_When_SequenceOrderIsZeroOrNegative(int invalidSequence)
    {
        // Arrange
        var request = new AddSlpLearningUnitRequest(
            LearningUnitId: Guid.NewGuid(),
            SequenceOrder: invalidSequence);

        // Act
        var result = _unitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddSlpLearningUnitRequest.SequenceOrder)
            && e.ErrorMessage.Contains("greater than zero"));
    }

    [Fact]
    public void UnitValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('N', 4001);
        var request = new AddSlpLearningUnitRequest(
            LearningUnitId: Guid.NewGuid(),
            SequenceOrder: 2,
            Notes: longNotes);

        // Act
        var result = _unitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddSlpLearningUnitRequest.Notes)
            && e.ErrorMessage.Contains("cannot exceed 4000 characters"));
    }

    #endregion

    #region SubmitStudentLearningPlanValidator Tests

    [Fact]
    public void SubmitValidator_Should_Succeed_When_NotesAreNull()
    {
        // Arrange
        var request = new SubmitStudentLearningPlanRequest(Notes: null);

        // Act
        var result = _submitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SubmitValidator_Should_Succeed_When_NotesAreProvidedAndValid()
    {
        // Arrange
        var request = new SubmitStudentLearningPlanRequest(
            Notes: "Ready for Academic Authority formal review and verification.");

        // Act
        var result = _submitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SubmitValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('S', 4001);
        var request = new SubmitStudentLearningPlanRequest(Notes: longNotes);

        // Act
        var result = _submitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SubmitStudentLearningPlanRequest.Notes)
            && e.ErrorMessage.Contains("must not exceed 4000 characters", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region ApproveStudentLearningPlanValidator Tests

    [Theory]
    [InlineData("initial_approval")]
    [InlineData("revision_approval")]
    [InlineData("INITIAL_APPROVAL")]
    [InlineData("  revision_approval  ")]
    public void ApproveValidator_Should_Succeed_When_ApprovalTypeIsValid(string approvalType)
    {
        // Arrange
        var request = new ApproveStudentLearningPlanRequest(
            ApprovalType: approvalType,
            Notes: "Formally approved by Curriculum Lead.");

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApproveValidator_Should_Fail_When_ApprovalTypeIsEmpty(string? approvalType)
    {
        // Arrange
        var request = new ApproveStudentLearningPlanRequest(
            ApprovalType: approvalType!,
            Notes: "Notes");

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ApproveStudentLearningPlanRequest.ApprovalType));
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("provisional")]
    [InlineData("unknown_approval")]
    public void ApproveValidator_Should_Fail_When_ApprovalTypeIsInvalid(string invalidType)
    {
        // Arrange
        var request = new ApproveStudentLearningPlanRequest(
            ApprovalType: invalidType,
            Notes: "Notes");

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ApproveStudentLearningPlanRequest.ApprovalType)
            && e.ErrorMessage.Contains("must be either 'initial_approval' or 'revision_approval'"));
    }

    [Fact]
    public void ApproveValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('A', 4001);
        var request = new ApproveStudentLearningPlanRequest(
            ApprovalType: "initial_approval",
            Notes: longNotes);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ApproveStudentLearningPlanRequest.Notes)
            && e.ErrorMessage.Contains("must not exceed 4000 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApproveValidator_Should_Succeed_When_NotesIsNull()
    {
        // Arrange
        var request = new ApproveStudentLearningPlanRequest(
            ApprovalType: "initial_approval",
            Notes: null);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ApproveValidator_Should_Succeed_When_NotesIsEmpty()
    {
        // Arrange
        var request = new ApproveStudentLearningPlanRequest(
            ApprovalType: "revision_approval",
            Notes: string.Empty);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ApproveValidator_Should_Succeed_When_NotesIsExactly4000Characters()
    {
        // Arrange
        var boundaryNotes = new string('A', 4000);
        var request = new ApproveStudentLearningPlanRequest(
            ApprovalType: "revision_approval",
            Notes: boundaryNotes);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ReviseStudentLearningPlanValidator Tests

    [Fact]
    public void ReviseValidator_Should_Succeed_When_NotesAreNull()
    {
        // Arrange
        var request = new ReviseStudentLearningPlanRequest(Notes: null);

        // Act
        var result = _reviseValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReviseValidator_Should_Succeed_When_NotesAreProvidedAndValid()
    {
        // Arrange
        var request = new ReviseStudentLearningPlanRequest(
            Notes: "Amending plan to introduce Grade 3 Remedial Math and adaptive physical education substitution.");

        // Act
        var result = _reviseValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReviseValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('R', 4001);
        var request = new ReviseStudentLearningPlanRequest(Notes: longNotes);

        // Act
        var result = _reviseValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReviseStudentLearningPlanRequest.Notes)
            && e.ErrorMessage.Contains("must not exceed 4000 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReviseValidator_Should_Succeed_When_NotesAreExactly4000Characters()
    {
        // Arrange
        var boundaryNotes = new string('R', 4000);
        var request = new ReviseStudentLearningPlanRequest(Notes: boundaryNotes);

        // Act
        var result = _reviseValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region DI Registration Tests

    [Fact]
    public void AddAcademicsDomain_Should_RegisterLearningPlansCommandHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAcademicsDomain();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(CreateStudentLearningPlanHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(UpdateStudentLearningPlanHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(ArchiveStudentLearningPlanHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(AddSlpLearningUnitHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(RemoveSlpLearningUnitHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(AddAcademicPlanAdjustmentHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(RemoveAcademicPlanAdjustmentHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(SubmitStudentLearningPlanHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(ApproveStudentLearningPlanHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(ReviseStudentLearningPlanHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(SyncSlpParticipationsHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
    }

    #endregion

    #region SyncSlpParticipations Tests

    [Fact]
    public void SyncSlpParticipationsResponse_Should_RetainAllAssignedProperties()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        const int syncedCount = 3;
        const int updatedCount = 1;
        const int totalUnits = 4;

        // Act
        var response = new SyncSlpParticipationsResponse(
            PlanId: planId,
            StudentId: studentId,
            SyncedCount: syncedCount,
            UpdatedCount: updatedCount,
            TotalUnits: totalUnits);

        // Assert
        response.PlanId.Should().Be(planId);
        response.StudentId.Should().Be(studentId);
        response.SyncedCount.Should().Be(syncedCount);
        response.UpdatedCount.Should().Be(updatedCount);
        response.TotalUnits.Should().Be(totalUnits);
    }

    #endregion
}
