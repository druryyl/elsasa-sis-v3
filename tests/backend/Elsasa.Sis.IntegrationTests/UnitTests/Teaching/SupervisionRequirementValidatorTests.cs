namespace Elsasa.Sis.IntegrationTests.UnitTests.Teaching;

using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.AddSupervisionRequirement;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.ResolveSupervisionRequirement;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class SupervisionRequirementValidatorTests
{
    private readonly AddSupervisionRequirementValidator _addValidator = new();
    private readonly ResolveSupervisionRequirementValidator _resolveValidator = new();

    [Theory]
    [InlineData("co_teaching")]
    [InlineData("periodic_observation")]
    [InlineData("lesson_plan_review")]
    [InlineData("general_mentorship")]
    public void AddValidator_Should_Pass_For_AllowedSupervisionTypes(string supervisionType)
    {
        // Arrange
        var request = new AddSupervisionRequirementRequest(
            SupervisionType: supervisionType,
            Description: "Wajib co-teaching pada semester 1 dengan guru senior.",
            StartDate: new DateOnly(2026, 7, 15),
            EndDate: new DateOnly(2026, 12, 20),
            SupervisingTeacherId: Guid.NewGuid());

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid_type")]
    [InlineData("unsupervised")]
    public void AddValidator_Should_Fail_For_InvalidSupervisionTypes(string? invalidType)
    {
        // Arrange
        var request = new AddSupervisionRequirementRequest(
            SupervisionType: invalidType!,
            Description: "Some description",
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SupervisionType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AddValidator_Should_Fail_When_DescriptionIsEmpty(string? description)
    {
        // Arrange
        var request = new AddSupervisionRequirementRequest(
            SupervisionType: "co_teaching",
            Description: description!,
            StartDate: new DateOnly(2026, 7, 15));

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void AddValidator_Should_Fail_When_EndDateIsBeforeStartDate()
    {
        // Arrange
        var request = new AddSupervisionRequirementRequest(
            SupervisionType: "co_teaching",
            Description: "Co-teaching mentorship",
            StartDate: new DateOnly(2026, 7, 15),
            EndDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate!.Value);
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("waived")]
    [InlineData("escalated")]
    [InlineData("COMPLETED")]
    public void ResolveValidator_Should_Pass_For_AllowedStatuses(string status)
    {
        // Arrange
        var request = new ResolveSupervisionRequirementRequest(
            ResolutionStatus: status,
            ResolutionNotes: "All co-teaching targets achieved with satisfactory performance.");

        // Act
        var result = _resolveValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ResolveValidator_Should_Pass_When_UsingStatusAliasProperty()
    {
        // Arrange
        var request = new ResolveSupervisionRequirementRequest(
            ResolutionStatus: string.Empty,
            ResolutionNotes: "Mentorship successfully completed.")
        {
            Status = "completed"
        };

        // Act
        var result = _resolveValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("active")]
    [InlineData("rejected")]
    [InlineData("invalid_status")]
    public void ResolveValidator_Should_Fail_For_InvalidStatuses(string? invalidStatus)
    {
        // Arrange
        var request = new ResolveSupervisionRequirementRequest(
            ResolutionStatus: invalidStatus!,
            ResolutionNotes: "Some resolution note");

        // Act
        var result = _resolveValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EffectiveStatus);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ResolveValidator_Should_Fail_When_ResolutionNotesIsEmpty(string? notes)
    {
        // Arrange
        var request = new ResolveSupervisionRequirementRequest(
            ResolutionStatus: "completed",
            ResolutionNotes: notes!);

        // Act
        var result = _resolveValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResolutionNotes);
    }
}
