namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;
using Elsasa.Sis.Academics.Features.LearningUnits.UpdateLearningUnit;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Learning Unit validators ensuring 100% rule coverage (P4-S11, ADR-005).
/// Covers <see cref="CreateLearningUnitValidator"/> and <see cref="UpdateLearningUnitValidator"/>.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class LearningUnitValidatorTests
{
    private readonly CreateLearningUnitValidator _createValidator = new();
    private readonly UpdateLearningUnitValidator _updateValidator = new();

    #region CreateLearningUnitValidator Tests

    [Fact]
    public void CreateValidator_Should_Succeed_When_AllFieldsAreValid()
    {
        // Arrange
        var request = new CreateLearningUnitRequest(
            ParentUnitId: Guid.NewGuid(),
            Code: "MATH-101",
            Name: "Mathematics Grade 1",
            UnitType: "subject",
            Level: "Grade 1",
            Description: "Fundamental mathematics curriculum",
            CreditWeight: 4.0m,
            Notes: "Core requirement");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CreateValidator_Should_Fail_When_CodeIsNullOrEmpty(string? code)
    {
        // Arrange
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: code!,
            Name: "Mathematics",
            UnitType: "subject");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningUnitRequest.Code)
            && e.ErrorMessage.Contains("Unit code is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_CodeExceeds50Characters()
    {
        // Arrange
        var longCode = new string('C', 51);
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: longCode,
            Name: "Mathematics",
            UnitType: "subject");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningUnitRequest.Code)
            && e.ErrorMessage.Contains("50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CreateValidator_Should_Fail_When_NameIsNullOrEmpty(string? name)
    {
        // Arrange
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: "BIO-101",
            Name: name!,
            UnitType: "course");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningUnitRequest.Name)
            && e.ErrorMessage.Contains("Unit name is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_NameExceeds200Characters()
    {
        // Arrange
        var longName = new string('N', 201);
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: "BIO-101",
            Name: longName,
            UnitType: "course");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningUnitRequest.Name)
            && e.ErrorMessage.Contains("200 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("subject")]
    [InlineData("course")]
    [InlineData("module")]
    [InlineData("pace")]
    [InlineData("project")]
    [InlineData("competency")]
    [InlineData("unit")]
    [InlineData("other")]
    [InlineData("SUBJECT")]
    [InlineData("  Course  ")]
    public void CreateValidator_Should_Succeed_For_AllAllowedUnitTypes(string unitType)
    {
        // Arrange
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: "ENG-01",
            Name: "English Foundations",
            UnitType: unitType);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    [InlineData("degree")]
    [InlineData("diploma")]
    [InlineData("invalid_type")]
    public void CreateValidator_Should_Fail_When_UnitTypeIsInvalidOrEmpty(string? unitType)
    {
        // Arrange
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: "ENG-01",
            Name: "English Foundations",
            UnitType: unitType!);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningUnitRequest.UnitType));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_LevelExceeds50Characters()
    {
        // Arrange
        var longLevel = new string('L', 51);
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: "ENG-01",
            Name: "English Foundations",
            UnitType: "subject",
            Level: longLevel);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningUnitRequest.Level)
            && e.ErrorMessage.Contains("50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.0)]
    [InlineData(-100)]
    public void CreateValidator_Should_Fail_When_CreditWeightIsNegative(decimal negativeWeight)
    {
        // Arrange
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: "ENG-01",
            Name: "English Foundations",
            UnitType: "subject",
            CreditWeight: negativeWeight);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningUnitRequest.CreditWeight)
            && e.ErrorMessage.Contains("greater than or equal to 0", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(4.0)]
    [InlineData(100.0)]
    public void CreateValidator_Should_Succeed_When_CreditWeightIsZeroOrPositive(decimal validWeight)
    {
        // Arrange
        var request = new CreateLearningUnitRequest(
            ParentUnitId: null,
            Code: "ENG-01",
            Name: "English Foundations",
            UnitType: "subject",
            CreditWeight: validWeight);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region UpdateLearningUnitValidator Tests

    [Fact]
    public void UpdateValidator_Should_Succeed_When_AllFieldsAreValid()
    {
        // Arrange
        var request = new UpdateLearningUnitRequest(
            ParentUnitId: Guid.NewGuid(),
            Code: "MATH-102",
            Name: "Advanced Mathematics",
            UnitType: "course",
            Level: "Grade 2",
            Description: "Advanced math syllabus",
            CreditWeight: 3.5m,
            Notes: "Updated credit");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void UpdateValidator_Should_Fail_When_CodeIsNullOrEmpty(string? code)
    {
        // Arrange
        var request = new UpdateLearningUnitRequest(
            ParentUnitId: null,
            Code: code!,
            Name: "Advanced Math",
            UnitType: "subject");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningUnitRequest.Code)
            && e.ErrorMessage.Contains("Unit code is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_CodeExceeds50Characters()
    {
        // Arrange
        var longCode = new string('C', 51);
        var request = new UpdateLearningUnitRequest(
            ParentUnitId: null,
            Code: longCode,
            Name: "Advanced Math",
            UnitType: "subject");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningUnitRequest.Code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void UpdateValidator_Should_Fail_When_NameIsNullOrEmpty(string? name)
    {
        // Arrange
        var request = new UpdateLearningUnitRequest(
            ParentUnitId: null,
            Code: "MATH-102",
            Name: name!,
            UnitType: "subject");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningUnitRequest.Name)
            && e.ErrorMessage.Contains("Unit name is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_NameExceeds200Characters()
    {
        // Arrange
        var longName = new string('N', 201);
        var request = new UpdateLearningUnitRequest(
            ParentUnitId: null,
            Code: "MATH-102",
            Name: longName,
            UnitType: "subject");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningUnitRequest.Name));
    }

    [Theory]
    [InlineData("subject")]
    [InlineData("course")]
    [InlineData("module")]
    [InlineData("pace")]
    [InlineData("project")]
    [InlineData("competency")]
    [InlineData("unit")]
    [InlineData("other")]
    public void UpdateValidator_Should_Succeed_For_AllAllowedUnitTypes(string unitType)
    {
        // Arrange
        var request = new UpdateLearningUnitRequest(
            ParentUnitId: null,
            Code: "MATH-102",
            Name: "Advanced Math",
            UnitType: unitType);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    [InlineData("random_type")]
    public void UpdateValidator_Should_Fail_When_UnitTypeIsInvalidOrEmpty(string? unitType)
    {
        // Arrange
        var request = new UpdateLearningUnitRequest(
            ParentUnitId: null,
            Code: "MATH-102",
            Name: "Advanced Math",
            UnitType: unitType!);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningUnitRequest.UnitType));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-5.0)]
    public void UpdateValidator_Should_Fail_When_CreditWeightIsNegative(decimal negativeWeight)
    {
        // Arrange
        var request = new UpdateLearningUnitRequest(
            ParentUnitId: null,
            Code: "MATH-102",
            Name: "Advanced Math",
            UnitType: "subject",
            CreditWeight: negativeWeight);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningUnitRequest.CreditWeight)
            && e.ErrorMessage.Contains("greater than or equal to 0", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
