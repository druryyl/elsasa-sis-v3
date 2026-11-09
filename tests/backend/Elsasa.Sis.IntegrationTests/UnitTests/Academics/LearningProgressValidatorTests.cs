namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation;
using Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress;
using FluentAssertions;
using FluentValidation.TestHelper;

/// <summary>
/// Unit test suite for Learning Progress command validators (P2-S04, UC-ACD-12).
/// Verifies categorical status constraints, operational basis enforcement, authorized decision note requirement, and bounds.
/// </summary>
public sealed class LearningProgressValidatorTests
{
    private readonly RecordLearningProgressValidator _recordValidator = new();
    private readonly ConcludeParticipationValidator _concludeValidator = new();

    #region RecordLearningProgressValidator Tests

    [Fact]
    public void RecordValidator_ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "in_progress",
            Basis: "learning_work",
            CompletionDate: null,
            Notes: "Student progressing well");

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RecordValidator_CompletedStatus_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "completed",
            Basis: "assessment_evidence",
            CompletionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Notes: "PACE unit completed");

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("not_started")]
    [InlineData("in_progress")]
    [InlineData("completed")]
    [InlineData("needs_remediation")]
    [InlineData("exempted")]
    [InlineData("IN_PROGRESS")]
    [InlineData("Completed")]
    public void RecordValidator_AllowedStatuses_ShouldPass(string status)
    {
        // Arrange
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: status,
            Basis: "assessment_evidence");

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("pending")]
    [InlineData("passed")]
    [InlineData("failed")]
    [InlineData("graduated")]
    public void RecordValidator_InvalidStatus_ShouldFail(string status)
    {
        // Arrange
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: status,
            Basis: "assessment_evidence");

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("assessment_evidence")]
    [InlineData("learning_work")]
    [InlineData("authorized_decision")]
    [InlineData("ASSESSMENT_EVIDENCE")]
    public void RecordValidator_AllowedBases_ShouldPass(string basis)
    {
        // Arrange
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "in_progress",
            Basis: basis,
            Notes: "Explanatory notes for validation");

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Basis);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("attendance")]
    [InlineData("teacher_opinion")]
    [InlineData("manual_override")]
    public void RecordValidator_InvalidBasis_ShouldFail(string basis)
    {
        // Arrange
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "in_progress",
            Basis: basis);

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Basis);
    }

    [Fact]
    public void RecordValidator_EmptyIdentifiers_ShouldFail()
    {
        // Arrange
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.Empty,
            LearningUnitId: Guid.Empty,
            AcademicPeriodId: Guid.Empty,
            Status: "in_progress",
            Basis: "learning_work");

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StudentId);
        result.ShouldHaveValidationErrorFor(x => x.LearningUnitId);
        result.ShouldHaveValidationErrorFor(x => x.AcademicPeriodId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordValidator_AuthorizedDecisionWithoutNotes_ShouldFail(string? notes)
    {
        // Arrange
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "completed",
            Basis: "authorized_decision",
            Notes: notes);

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Explanatory notes are required when basis is 'authorized_decision'.");
    }

    [Fact]
    public void RecordValidator_NotesExceedingLimit_ShouldFail()
    {
        // Arrange
        var excessiveNotes = new string('a', 1001);
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "in_progress",
            Basis: "learning_work",
            Notes: excessiveNotes);

        // Act
        var result = _recordValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1000 characters.");
    }

    #endregion

    #region ConcludeParticipationValidator Tests

    [Fact]
    public void ConcludeValidator_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ConcludeParticipationCommand(
            ParticipationId: Guid.NewGuid(),
            CompletionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Notes: "Completed on schedule");

        // Act
        var result = _concludeValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ConcludeValidator_EmptyParticipationId_ShouldFail()
    {
        // Arrange
        var command = new ConcludeParticipationCommand(
            ParticipationId: Guid.Empty);

        // Act
        var result = _concludeValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipationId)
            .WithErrorMessage("Participation ID is required.");
    }

    [Fact]
    public void ConcludeValidator_NotesExceedingLimit_ShouldFail()
    {
        // Arrange
        var excessiveNotes = new string('x', 1001);
        var command = new ConcludeParticipationCommand(
            ParticipationId: Guid.NewGuid(),
            Notes: excessiveNotes);

        // Act
        var result = _concludeValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1000 characters.");
    }

    #endregion
}
