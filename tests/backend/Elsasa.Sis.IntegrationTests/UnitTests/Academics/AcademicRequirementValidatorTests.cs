namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;
using Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Academic Requirement validators ensuring full rule coverage (P4-S11, ADR-005).
/// Covers <see cref="CreateAcademicRequirementValidator"/> and <see cref="UpdateAcademicRequirementValidator"/>.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class AcademicRequirementValidatorTests
{
    private readonly CreateAcademicRequirementValidator _createValidator = new();
    private readonly UpdateAcademicRequirementValidator _updateValidator = new();

    #region CreateAcademicRequirementValidator Tests

    [Fact]
    public void CreateValidator_Should_Succeed_When_ProgramScopeRequirementIsValid()
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Minimum Attendance Policy",
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: "Students must maintain at least 85% attendance across all units.",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1),
            EndDate: new DateOnly(2027, 6, 30),
            Notes: "Campus-wide academic policy");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_LevelScopeRequirementProvidesLevel()
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Grade 4 Credit Requirement",
            RequirementScope: "level",
            LearningUnitId: null,
            Level: "Grade 4",
            RequirementType: "credit",
            Description: "Grade 4 students must complete at least 32 credits.",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_UnitScopeRequirementProvidesLearningUnitId()
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Math 101 Mastery Requirement",
            RequirementScope: "learning_unit",
            LearningUnitId: Guid.NewGuid(),
            Level: null,
            RequirementType: "prerequisite",
            Description: "Must achieve at least 75% score before proceeding.",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("program")]
    [InlineData("level")]
    [InlineData("learning_unit")]
    [InlineData("PROGRAM")]
    [InlineData("  Learning_Unit  ")]
    public void CreateValidator_Should_Accept_ValidRequirementScopes(string scope)
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "General Requirement",
            RequirementScope: scope,
            LearningUnitId: Guid.NewGuid(),
            Level: "Grade 1",
            RequirementType: "completion",
            Description: "Standard requirement description",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    [InlineData("department")]
    [InlineData("course_only")]
    [InlineData("invalid_scope")]
    public void CreateValidator_Should_Fail_When_RequirementScopeIsInvalid(string? scope)
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "General Requirement",
            RequirementScope: scope!,
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: "Standard requirement description",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.RequirementScope));
    }

    [Theory]
    [InlineData("completion")]
    [InlineData("prerequisite")]
    [InlineData("credit")]
    [InlineData("progression")]
    [InlineData("graduation")]
    [InlineData("assessment_policy")]
    [InlineData("COMPLETION")]
    [InlineData("  Prerequisite  ")]
    public void CreateValidator_Should_Accept_ValidRequirementTypes(string type)
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "General Requirement",
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: type,
            Description: "Standard requirement description",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    [InlineData("attendance")]
    [InlineData("disciplinary")]
    [InlineData("unknown_type")]
    public void CreateValidator_Should_Fail_When_RequirementTypeIsInvalid(string? type)
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "General Requirement",
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: type!,
            Description: "Standard requirement description",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.RequirementType));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_UnitScopeOmitsLearningUnitId()
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Unit Mastery Requirement",
            RequirementScope: "learning_unit",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: "Unit must be completed with 75% score.",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.LearningUnitId)
            && e.ErrorMessage.Contains("Learning unit ID is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_UnitScopeSuppliesEmptyGuid()
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Unit Mastery Requirement",
            RequirementScope: "learning_unit",
            LearningUnitId: Guid.Empty,
            Level: null,
            RequirementType: "completion",
            Description: "Unit must be completed with 75% score.",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.LearningUnitId)
            && e.ErrorMessage.Contains("must not be empty", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CreateValidator_Should_Fail_When_LevelScopeOmitsLevel(string? level)
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Level Requirement",
            RequirementScope: "level",
            LearningUnitId: null,
            Level: level!,
            RequirementType: "credit",
            Description: "Credit quota for level",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.Level)
            && e.ErrorMessage.Contains("Level is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_LevelExceeds50Characters()
    {
        // Arrange
        var longLevel = new string('L', 51);
        var request = new CreateAcademicRequirementRequest(
            Name: "Level Requirement",
            RequirementScope: "level",
            LearningUnitId: null,
            Level: longLevel,
            RequirementType: "credit",
            Description: "Credit quota for level",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.Level)
            && e.ErrorMessage.Contains("50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CreateValidator_Should_Fail_When_NameIsNullOrEmpty(string? name)
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: name!,
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: "Valid description",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.Name));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_NameExceeds200Characters()
    {
        // Arrange
        var longName = new string('N', 201);
        var request = new CreateAcademicRequirementRequest(
            Name: longName,
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: "Valid description",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.Name)
            && e.ErrorMessage.Contains("200 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CreateValidator_Should_Fail_When_DescriptionIsNullOrEmpty(string? description)
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Valid Name",
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: description!,
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.Description));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_DescriptionExceeds4000Characters()
    {
        // Arrange
        var longDesc = new string('D', 4001);
        var request = new CreateAcademicRequirementRequest(
            Name: "Valid Name",
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: longDesc,
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.Description));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CreateValidator_Should_Fail_When_VersionIsNullOrEmpty(string? version)
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Valid Name",
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: "Valid description",
            Version: version!,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.Version));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_EffectiveDateIsDefault()
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Valid Name",
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: "Valid description",
            Version: "1.0",
            EffectiveDate: default);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.EffectiveDate));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_EndDateIsEarlierThanEffectiveDate()
    {
        // Arrange
        var request = new CreateAcademicRequirementRequest(
            Name: "Valid Name",
            RequirementScope: "program",
            LearningUnitId: null,
            Level: null,
            RequirementType: "completion",
            Description: "Valid description",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1),
            EndDate: new DateOnly(2026, 6, 30));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicRequirementRequest.EndDate)
            && e.ErrorMessage.Contains("greater than or equal to effective date", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region UpdateAcademicRequirementValidator Tests

    [Fact]
    public void UpdateValidator_Should_Succeed_When_FieldsAreValid()
    {
        // Arrange
        var request = new UpdateAcademicRequirementRequest(
            Name: "Updated Policy Name",
            Description: "Updated detailed policy text.",
            Notes: "Reviewed policy",
            RequirementScope: "level",
            RequirementType: "progression",
            Level: "Grade 3",
            Version: "1.1",
            EffectiveDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));

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
    public void UpdateValidator_Should_Fail_When_NameIsNullOrEmpty(string? name)
    {
        // Arrange
        var request = new UpdateAcademicRequirementRequest(
            Name: name!,
            Description: "Valid description");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAcademicRequirementRequest.Name));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_NameExceeds200Characters()
    {
        // Arrange
        var longName = new string('N', 201);
        var request = new UpdateAcademicRequirementRequest(
            Name: longName,
            Description: "Valid description");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAcademicRequirementRequest.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void UpdateValidator_Should_Fail_When_DescriptionIsNullOrEmpty(string? description)
    {
        // Arrange
        var request = new UpdateAcademicRequirementRequest(
            Name: "Valid Name",
            Description: description!);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAcademicRequirementRequest.Description));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_DescriptionExceeds4000Characters()
    {
        // Arrange
        var longDesc = new string('D', 4001);
        var request = new UpdateAcademicRequirementRequest(
            Name: "Valid Name",
            Description: longDesc);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAcademicRequirementRequest.Description));
    }

    [Theory]
    [InlineData("invalid_scope")]
    [InlineData("institution")]
    public void UpdateValidator_Should_Fail_When_RequirementScopeIsInvalid(string invalidScope)
    {
        // Arrange
        var request = new UpdateAcademicRequirementRequest(
            Name: "Valid Name",
            Description: "Valid description",
            RequirementScope: invalidScope);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAcademicRequirementRequest.RequirementScope));
    }

    [Theory]
    [InlineData("invalid_type")]
    [InlineData("behavior")]
    public void UpdateValidator_Should_Fail_When_RequirementTypeIsInvalid(string invalidType)
    {
        // Arrange
        var request = new UpdateAcademicRequirementRequest(
            Name: "Valid Name",
            Description: "Valid description",
            RequirementType: invalidType);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAcademicRequirementRequest.RequirementType));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_EndDateIsEarlierThanEffectiveDate()
    {
        // Arrange
        var request = new UpdateAcademicRequirementRequest(
            Name: "Valid Name",
            Description: "Valid description",
            EffectiveDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2026, 7, 31));

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAcademicRequirementRequest.EndDate)
            && e.ErrorMessage.Contains("greater than or equal to effective date", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
