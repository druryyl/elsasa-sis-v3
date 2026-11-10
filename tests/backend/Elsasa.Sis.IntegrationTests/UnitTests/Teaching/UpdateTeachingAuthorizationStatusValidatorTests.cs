namespace Elsasa.Sis.IntegrationTests.UnitTests.Teaching;

using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.UpdateTeachingAuthorizationStatus;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class UpdateTeachingAuthorizationStatusValidatorTests
{
    private readonly UpdateTeachingAuthorizationStatusValidator _validator = new();

    [Theory]
    [InlineData("active")]
    [InlineData("authorized_under_supervision")]
    [InlineData("restricted")]
    [InlineData("suspended")]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("ACTIVE")]
    [InlineData("Restricted")]
    [InlineData(" active ")]
    public void Validate_Should_Pass_When_StatusIsAllowedAndReasonProvided(string status)
    {
        // Arrange
        var request = new UpdateTeachingAuthorizationStatusRequest(status, "Legitimate administrative justification.");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("pending_approval")]
    [InlineData("unknown")]
    [InlineData("cancelled")]
    [InlineData("123")]
    public void Validate_Should_Fail_When_StatusIsNotSupported(string status)
    {
        // Arrange
        var request = new UpdateTeachingAuthorizationStatusRequest(status, "Reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage("Status must be one of: 'active', 'authorized_under_supervision', 'restricted', 'suspended', 'revoked', 'expired'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_StatusIsEmptyOrNull(string? status)
    {
        // Arrange
        var request = new UpdateTeachingAuthorizationStatusRequest(status, "Reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_StatusReasonIsEmptyOrNull(string? reason)
    {
        // Arrange
        var request = new UpdateTeachingAuthorizationStatusRequest("restricted", reason);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StatusReason)
            .WithErrorMessage("Status justification reason is mandatory.");
    }

    [Fact]
    public void Validate_Should_Pass_When_AliasReasonIsProvided()
    {
        // Arrange
        var request = new UpdateTeachingAuthorizationStatusRequest(Status: "revoked", Reason: "Revocation due to policy breach.");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_SuspensionOrRevocationReasonIsProvided()
    {
        // Arrange
        var request = new UpdateTeachingAuthorizationStatusRequest(Status: "suspended", SuspensionOrRevocationReason: "Under formal investigation.");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
