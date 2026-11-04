namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

public sealed class BatchAssessObligationsValidatorTests
{
    private readonly BatchAssessObligationsValidator _validator = new();

    [Theory]
    [InlineData("preview")]
    [InlineData("commit")]
    [InlineData("PREVIEW")]
    [InlineData("Commit")]
    public void Validate_ValidRequest_ShouldPassValidation(string mode)
    {
        // Arrange
        var request = new BatchAssessObligationsRequest(
            ChargeRuleId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            BillingCycleLabel: "Cycle 2026-11 Monthly Tuition",
            TargetCampus: "Kampus Utama",
            TargetGrade: "Grade 1",
            Mode: mode);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyChargeRuleId_ShouldFailValidation()
    {
        // Arrange
        var request = new BatchAssessObligationsRequest(
            ChargeRuleId: Guid.Empty,
            AcademicPeriodId: Guid.NewGuid(),
            BillingCycleLabel: "Cycle 2026-11",
            TargetCampus: null,
            TargetGrade: null,
            Mode: "preview");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.ChargeRuleId));
    }

    [Fact]
    public void Validate_EmptyAcademicPeriodId_ShouldFailValidation()
    {
        // Arrange
        var request = new BatchAssessObligationsRequest(
            ChargeRuleId: Guid.NewGuid(),
            AcademicPeriodId: Guid.Empty,
            BillingCycleLabel: "Cycle 2026-11",
            TargetCampus: null,
            TargetGrade: null,
            Mode: "preview");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.AcademicPeriodId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyBillingCycleLabel_ShouldFailValidation(string? label)
    {
        // Arrange
        var request = new BatchAssessObligationsRequest(
            ChargeRuleId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            BillingCycleLabel: label!,
            TargetCampus: null,
            TargetGrade: null,
            Mode: "preview");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.BillingCycleLabel));
    }

    [Fact]
    public void Validate_BillingCycleLabelExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var request = new BatchAssessObligationsRequest(
            ChargeRuleId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            BillingCycleLabel: new string('B', 101),
            TargetCampus: null,
            TargetGrade: null,
            Mode: "preview");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.BillingCycleLabel));
    }

    [Theory]
    [InlineData("execute")]
    [InlineData("dry-run")]
    [InlineData("test")]
    [InlineData("invalid")]
    [InlineData("")]
    public void Validate_InvalidMode_ShouldFailValidation(string mode)
    {
        // Arrange
        var request = new BatchAssessObligationsRequest(
            ChargeRuleId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            BillingCycleLabel: "Cycle 2026-11",
            TargetCampus: null,
            TargetGrade: null,
            Mode: mode);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Mode));
    }

    [Fact]
    public void Validate_TargetCampusExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var request = new BatchAssessObligationsRequest(
            ChargeRuleId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            BillingCycleLabel: "Cycle 2026-11",
            TargetCampus: new string('C', 101),
            TargetGrade: null,
            Mode: "preview");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.TargetCampus));
    }

    [Fact]
    public void Validate_TargetGradeExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var request = new BatchAssessObligationsRequest(
            ChargeRuleId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            BillingCycleLabel: "Cycle 2026-11",
            TargetCampus: null,
            TargetGrade: new string('G', 65),
            Mode: "preview");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.TargetGrade));
    }
}
