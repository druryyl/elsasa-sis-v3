namespace Elsasa.Sis.IntegrationTests.UnitTests.Teaching;

using Elsasa.Sis.Teaching.Features.TeacherProfiles.ChangeTeacherStanding;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class ChangeTeacherStandingValidatorTests
{
    private readonly ChangeTeacherStandingValidator _validator = new();

    [Theory]
    [InlineData("active")]
    [InlineData("restricted")]
    [InlineData("suspended")]
    [InlineData("inactive")]
    [InlineData("ACTIVE")]
    [InlineData("Restricted")]
    [InlineData(" active ")]
    public void Validate_Should_Pass_When_StandingIsAllowedAndReasonProvided(string standing)
    {
        // Arrange
        var request = new ChangeTeacherStandingRequest(standing, "Legitimate administrative justification.");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("pending_approval")]
    [InlineData("unknown")]
    [InlineData("graduated")]
    [InlineData("123")]
    public void Validate_Should_Fail_When_StandingIsNotSupported(string standing)
    {
        // Arrange
        var request = new ChangeTeacherStandingRequest(standing, "Reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Standing)
            .WithErrorMessage("Standing must be one of: 'active', 'restricted', 'suspended', 'inactive'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_StandingIsEmptyOrNull(string? standing)
    {
        // Arrange
        var request = new ChangeTeacherStandingRequest(standing!, "Reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Standing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_StandingReasonIsEmptyOrNull(string? reason)
    {
        // Arrange
        var request = new ChangeTeacherStandingRequest("active", reason!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StandingReason)
            .WithErrorMessage("Standing justification reason is mandatory.");
    }
}
