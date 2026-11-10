namespace Elsasa.Sis.IntegrationTests.UnitTests.Teaching;

using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.GrantTeachingAuthorization;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class GrantTeachingAuthorizationValidatorTests
{
    private readonly GrantTeachingAuthorizationValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_ValidActiveAuthorizationRequest()
    {
        // Arrange
        var request = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Matematika SD Kelas 4-6 Tatap Muka",
            CampusScope: "Kampus Utama",
            ProgramScope: "Primary",
            LevelScope: "Grades 4-6",
            DeliveryModeScope: "in_person",
            EffectiveStartDate: new DateOnly(2026, 7, 1),
            EffectiveEndDate: new DateOnly(2027, 6, 30),
            Status: "active");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_UsingPromptAliasFields()
    {
        // Arrange
        var request = new GrantTeachingAuthorizationRequest(
            AuthorizationScope: "Bahasa Inggris SMP",
            CampusBoundary: "Kampus Barat",
            EducationalProgramScope: "Junior High",
            GradeLevelScope: "Grade 7",
            EffectiveFrom: new DateOnly(2026, 7, 1),
            EffectiveTo: new DateOnly(2027, 6, 30));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_ScopeDescriptionIsEmpty(string? scope)
    {
        // Arrange
        var request = new GrantTeachingAuthorizationRequest(
            ScopeDescription: scope,
            EffectiveStartDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResolvedScopeDescription)
            .WithErrorMessage("Authorization scope description is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_EndDateIsBeforeStartDate()
    {
        // Arrange
        var request = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Scope",
            EffectiveStartDate: new DateOnly(2026, 7, 1),
            EffectiveEndDate: new DateOnly(2026, 6, 30));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResolvedEffectiveEndDate)
            .WithErrorMessage("Effective end date must be on or after the effective start date.");
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("suspended")]
    [InlineData("expired")]
    [InlineData("unknown_status")]
    public void Validate_Should_Fail_When_StatusIsNotGrantable(string status)
    {
        // Arrange
        var request = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Scope",
            EffectiveStartDate: new DateOnly(2026, 7, 1),
            Status: status);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResolvedStatus)
            .WithErrorMessage("Authorization status must be either 'active' or 'authorized_under_supervision'.");
    }

    [Fact]
    public void Validate_Should_Fail_When_SupervisedStatus_MissingSupervisionPayload()
    {
        // Arrange
        var request = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Scope",
            EffectiveStartDate: new DateOnly(2026, 7, 1),
            Status: "authorized_under_supervision");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResolvedSupervision)
            .WithErrorMessage("Supervision requirement details are mandatory when authorization status is 'authorized_under_supervision'.");
    }

    [Theory]
    [InlineData("invalid_supervision")]
    [InlineData("random_mentorship")]
    public void Validate_Should_Fail_When_SupervisionTypeIsInvalid(string supervisionType)
    {
        // Arrange
        var request = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Scope",
            EffectiveStartDate: new DateOnly(2026, 7, 1),
            Status: "authorized_under_supervision",
            Supervision: new GrantSupervisionPayload(
                SupervisionType: supervisionType,
                Description: "Mentoring plan",
                StartDate: new DateOnly(2026, 7, 1)));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResolvedSupervision!.SupervisionType)
            .WithErrorMessage("Supervision type must be one of: 'co_teaching', 'periodic_observation', 'lesson_plan_review', 'general_mentorship'.");
    }

    [Theory]
    [InlineData("co_teaching")]
    [InlineData("periodic_observation")]
    [InlineData("lesson_plan_review")]
    [InlineData("general_mentorship")]
    public void Validate_Should_Pass_When_SupervisedStatus_WithValidSupervisionPayload(string supervisionType)
    {
        // Arrange
        var request = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Scope",
            EffectiveStartDate: new DateOnly(2026, 7, 1),
            Status: "authorized_under_supervision",
            Supervision: new GrantSupervisionPayload(
                SupervisionType: supervisionType,
                Description: "Mentoring plan",
                StartDate: new DateOnly(2026, 7, 1),
                EndDate: new DateOnly(2026, 12, 31),
                SupervisingTeacherId: Guid.NewGuid()));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
