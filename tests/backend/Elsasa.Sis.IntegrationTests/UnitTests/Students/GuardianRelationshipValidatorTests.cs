namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Records.Guardians;
using Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;
using Elsasa.Sis.Students.Features.Records.Guardians.UpdateGuardianRelationship;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class GuardianRelationshipValidatorTests
{
    private readonly AddGuardianRelationshipValidator _addValidator = new();
    private readonly UpdateGuardianRelationshipValidator _updateValidator = new();

    #region AddGuardianRelationshipValidator Tests

    [Theory]
    [InlineData("mother")]
    [InlineData("father")]
    [InlineData("legal_guardian")]
    [InlineData("legal guardian")]
    [InlineData("grandparent")]
    [InlineData("sibling")]
    [InlineData("other")]
    [InlineData("Mother")]
    [InlineData("FATHER")]
    [InlineData("Legal Guardian")]
    public void AddValidator_Should_Succeed_When_RelationshipTypeIsValid(string relationshipType)
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Budi Santoso",
            RelationshipType: relationshipType);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RelationshipType);
    }

    [Theory]
    [InlineData("friend")]
    [InlineData("neighbor")]
    [InlineData("cousin")]
    [InlineData("uncle")]
    [InlineData("aunt")]
    [InlineData("unknown")]
    public void AddValidator_Should_Fail_When_RelationshipTypeIsInvalid(string invalidType)
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Budi Santoso",
            RelationshipType: invalidType);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RelationshipType)
            .WithErrorMessage("Relationship type must be a valid relationship (mother, father, legal_guardian, grandparent, sibling, other).");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddValidator_Should_Fail_When_RelationshipTypeIsEmptyOrWhitespace(string? relationshipType)
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Budi Santoso",
            RelationshipType: relationshipType!);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RelationshipType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddValidator_Should_Fail_When_FullNameIsEmptyOrWhitespace(string? fullName)
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: fullName!,
            RelationshipType: "mother");

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Guardian full name is required.");
    }

    [Fact]
    public void AddValidator_Should_Fail_When_FullNameExceeds200Characters()
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: new string('A', 201),
            RelationshipType: "mother");

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Guardian full name cannot exceed 200 characters.");
    }

    [Fact]
    public void AddValidator_Should_Succeed_When_AllFieldsAreValid()
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: Guid.NewGuid(),
            FullName: "Siti Rahmawati",
            RelationshipType: "mother",
            GovernmentIdType: "NIK",
            GovernmentIdNumber: "3171019876543210",
            Phone: "+62 812-3456-7890",
            Email: "siti.rahmawati@example.com",
            Address: "Jl. Melati No. 12, Bandung",
            Occupation: "Teacher",
            IsPrimaryContact: true,
            IsEmergencyContact: true,
            IsFinancialContact: true,
            IsAuthorizedPickup: true);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("+628123456789")]
    [InlineData("0812-3456-7890")]
    [InlineData("(021) 123456")]
    [InlineData("+1 (555) 019-2834")]
    public void AddValidator_Should_Accept_ValidPhoneFormats(string validPhone)
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Valid Name",
            RelationshipType: "father",
            Phone: validPhone);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Theory]
    [InlineData("invalid-phone")]
    [InlineData("123#456")]
    [InlineData("phone@number")]
    public void AddValidator_Should_Fail_When_PhoneFormatIsInvalid(string invalidPhone)
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Valid Name",
            RelationshipType: "father",
            Phone: invalidPhone);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Guardian phone number format is invalid.");
    }

    [Fact]
    public void AddValidator_Should_Fail_When_PhoneExceeds32Characters()
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Valid Name",
            RelationshipType: "father",
            Phone: new string('9', 33));

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Guardian phone cannot exceed 32 characters.");
    }

    [Fact]
    public void AddValidator_Should_Fail_When_EmailFormatIsInvalid()
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Valid Name",
            RelationshipType: "father",
            Email: "not-an-email");

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Guardian email must be a valid email address.");
    }

    [Fact]
    public void AddValidator_Should_Fail_When_EmailExceeds254Characters()
    {
        // Arrange
        var longEmail = $"{new string('a', 245)}@domain.com";
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Valid Name",
            RelationshipType: "father",
            Email: longEmail);

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Guardian email cannot exceed 254 characters.");
    }

    [Fact]
    public void AddValidator_Should_Fail_When_OccupationExceeds100Characters()
    {
        // Arrange
        var request = new AddGuardianRelationshipRequest(
            GuardianId: null,
            FullName: "Valid Name",
            RelationshipType: "father",
            Occupation: new string('O', 101));

        // Act
        var result = _addValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Occupation)
            .WithErrorMessage("Guardian occupation cannot exceed 100 characters.");
    }

    #endregion

    #region UpdateGuardianRelationshipValidator Tests

    [Theory]
    [InlineData("mother")]
    [InlineData("father")]
    [InlineData("legal_guardian")]
    [InlineData("legal guardian")]
    [InlineData("grandparent")]
    [InlineData("sibling")]
    [InlineData("other")]
    public void UpdateValidator_Should_Succeed_When_RelationshipTypeIsValid(string relationshipType)
    {
        // Arrange
        var request = new UpdateGuardianRelationshipRequest(
            RelationshipType: relationshipType);

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RelationshipType);
    }

    [Theory]
    [InlineData("friend")]
    [InlineData("coach")]
    [InlineData("nanny")]
    public void UpdateValidator_Should_Fail_When_RelationshipTypeIsInvalid(string invalidType)
    {
        // Arrange
        var request = new UpdateGuardianRelationshipRequest(
            RelationshipType: invalidType);

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RelationshipType)
            .WithErrorMessage("Relationship type must be a valid relationship (mother, father, legal_guardian, grandparent, sibling, other).");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateValidator_Should_Fail_When_RelationshipTypeIsEmptyOrWhitespace(string? relationshipType)
    {
        // Arrange
        var request = new UpdateGuardianRelationshipRequest(
            RelationshipType: relationshipType!);

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RelationshipType);
    }

    [Fact]
    public void UpdateValidator_Should_Succeed_When_AllFieldsAreValid()
    {
        // Arrange
        var request = new UpdateGuardianRelationshipRequest(
            RelationshipType: "legal_guardian",
            Phone: "+628111222333",
            Email: "legal.guardian@example.com",
            Occupation: "Attorney",
            IsPrimaryContact: true,
            IsEmergencyContact: true,
            IsFinancialContact: false,
            IsAuthorizedPickup: true,
            IsActive: true);

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_PhoneFormatIsInvalid()
    {
        // Arrange
        var request = new UpdateGuardianRelationshipRequest(
            RelationshipType: "father",
            Phone: "invalid_phone#123");

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Guardian phone number format is invalid.");
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_EmailFormatIsInvalid()
    {
        // Arrange
        var request = new UpdateGuardianRelationshipRequest(
            RelationshipType: "father",
            Email: "not-an-email");

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Guardian email must be a valid email address.");
    }

    #endregion

    #region GuardianRelationshipTypes Helper Tests

    [Theory]
    [InlineData("mother", true)]
    [InlineData("father", true)]
    [InlineData("legal_guardian", true)]
    [InlineData("legal guardian", true)]
    [InlineData("grandparent", true)]
    [InlineData("sibling", true)]
    [InlineData("other", true)]
    [InlineData("Mother", true)]
    [InlineData("FATHER", true)]
    [InlineData("Legal Guardian", true)]
    [InlineData("friend", false)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void GuardianRelationshipTypes_IsValid_Should_EvaluateCorrectly(string? input, bool expected)
    {
        // Act
        var actual = GuardianRelationshipTypes.IsValid(input);

        // Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("legal guardian", "legal_guardian")]
    [InlineData("Legal Guardian", "legal_guardian")]
    [InlineData("LEGAL GUARDIAN", "legal_guardian")]
    [InlineData("Mother", "mother")]
    [InlineData("  father  ", "father")]
    [InlineData("Grandparent", "grandparent")]
    public void GuardianRelationshipTypes_Normalize_Should_ReturnExpectedNormalizedString(string input, string expected)
    {
        // Act
        var actual = GuardianRelationshipTypes.Normalize(input);

        // Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GuardianRelationshipTypes_Normalize_Should_ThrowArgumentException_When_NullOrWhitespace(string? input)
    {
        // Act
        var act = () => GuardianRelationshipTypes.Normalize(input!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion
}
