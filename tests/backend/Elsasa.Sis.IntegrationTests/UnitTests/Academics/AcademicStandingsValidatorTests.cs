namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;
using Elsasa.Sis.Academics.Features.AcademicStandings.DetermineAcademicStanding;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

/// <summary>
/// Unit test suite for single and batch academic standing determination command validators (P3-S07, UC-ACD-17).
/// Verifies required fields, standing enum constraints, conditional standing rules, and committee exception requirements.
/// </summary>
public sealed class AcademicStandingsValidatorTests
{
    private readonly DetermineAcademicStandingValidator _singleValidator = new();
    private readonly BatchDetermineCohortStandingValidator _batchValidator = new();
    private readonly StandingItemDtoValidator _itemValidator = new();

    #region Single DetermineAcademicStandingValidator Tests

    [Fact]
    public void SingleValidator_ValidGoodStanding_ShouldPass()
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SingleValidator_ValidConditionalStanding_WithConditions_ShouldPass()
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "conditional_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: "Must complete Mathematics remediation during holiday packet.",
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
    public void SingleValidator_ConditionalStanding_MissingConditions_ShouldFail(string? conditions)
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "conditional_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: conditions,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Conditions)
            .WithErrorMessage("Conditions are mandatory when standing is 'conditional_standing'.");
    }

    [Fact]
    public void SingleValidator_ValidException_WithReasonAndDecree_ShouldPass()
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: true,
            ExceptionReason: "Granted exception following medical committee evaluation.",
            CommitteeReference: "SK-DG/2026/11-04");

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("Abc")] // < 5 chars
    public void SingleValidator_IsExceptionTrue_InvalidReason_ShouldFail(string? reason)
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: true,
            ExceptionReason: reason,
            CommitteeReference: "SK-DG/2026/11-04");

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SK1")] // < 5 chars
    public void SingleValidator_IsExceptionTrue_InvalidCommitteeReference_ShouldFail(string? committeeRef)
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: true,
            ExceptionReason: "Granted medical accommodation.",
            CommitteeReference: committeeRef);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    [Fact]
    public void SingleValidator_EmptyStudentId_ShouldFail()
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.Empty,
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01");

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Fact]
    public void SingleValidator_EmptyAcademicPeriodId_ShouldFail()
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.Empty,
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01");

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AcademicPeriodId);
    }

    [Theory]
    [InlineData("invalid_standing")]
    [InlineData("probation")]
    [InlineData("")]
    public void SingleValidator_InvalidStanding_ShouldFail(string standing)
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: standing,
            PolicyReference: "SK-AKAD/2026/POLICY-01");

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Standing);
    }

    [Theory]
    [InlineData("good_standing")]
    [InlineData("Good_Standing")]
    [InlineData("conditional_standing")]
    [InlineData("not_in_good_standing")]
    public void SingleValidator_ValidStandingValues_ShouldPass(string standing)
    {
        // Arrange
        var command = new DetermineAcademicStandingCommand(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: standing,
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: standing.Contains("conditional", StringComparison.OrdinalIgnoreCase) ? "Mandatory conditions" : null);

        // Act
        var result = _singleValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Standing);
    }

    #endregion

    #region Batch & StandingItemDtoValidator Tests

    [Fact]
    public void BatchValidator_ValidBatchCommand_ShouldPass()
    {
        // Arrange
        var items = new List<StandingItemDto>
        {
            new(Guid.NewGuid(), "good_standing", "SK-AKAD/2026/POLICY-01"),
            new(Guid.NewGuid(), "conditional_standing", "SK-AKAD/2026/POLICY-01", Conditions: "Must complete Math packet"),
            new(Guid.NewGuid(), "good_standing", "SK-AKAD/2026/POLICY-01", IsException: true, ExceptionReason: "Approved medical decree", CommitteeReference: "SK-DG/2026/11-04")
        };

        var command = new BatchDetermineCohortStandingCommand(Guid.NewGuid(), items);

        // Act
        var result = _batchValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void BatchValidator_EmptyPeriodId_ShouldFail()
    {
        // Arrange
        var command = new BatchDetermineCohortStandingCommand(Guid.Empty, new List<StandingItemDto>
        {
            new(Guid.NewGuid(), "good_standing", "SK-AKAD/2026/POLICY-01")
        });

        // Act
        var result = _batchValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AcademicPeriodId);
    }

    [Fact]
    public void BatchValidator_EmptyItemsList_ShouldFail()
    {
        // Arrange
        var command = new BatchDetermineCohortStandingCommand(Guid.NewGuid(), Array.Empty<StandingItemDto>());

        // Act
        var result = _batchValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void ItemValidator_ValidItem_ShouldPass()
    {
        // Arrange
        var item = new StandingItemDto(
            Guid.NewGuid(),
            "not_in_good_standing",
            "SK-AKAD/2026/POLICY-01");

        // Act
        var result = _itemValidator.TestValidate(item);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ItemValidator_ConditionalWithoutConditions_ShouldFail()
    {
        // Arrange
        var item = new StandingItemDto(
            Guid.NewGuid(),
            "conditional_standing",
            "SK-AKAD/2026/POLICY-01",
            Conditions: null);

        // Act
        var result = _itemValidator.TestValidate(item);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Conditions)
            .WithErrorMessage("Conditions are mandatory when standing is 'conditional_standing'.");
    }

    [Fact]
    public void ItemValidator_ExceptionWithoutReasonOrRef_ShouldFail()
    {
        // Arrange
        var item = new StandingItemDto(
            Guid.NewGuid(),
            "good_standing",
            "SK-AKAD/2026/POLICY-01",
            IsException: true,
            ExceptionReason: "Too",
            CommitteeReference: null);

        // Act
        var result = _itemValidator.TestValidate(item);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    #endregion
}
