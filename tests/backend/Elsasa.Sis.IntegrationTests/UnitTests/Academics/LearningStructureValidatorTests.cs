namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.SupersedeLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Learning Structure validators ensuring full rule coverage (P4-S11, ADR-005).
/// Covers <see cref="CreateLearningStructureValidator"/>, <see cref="UpdateLearningStructureValidator"/>,
/// and <see cref="SupersedeStructureValidator"/>.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class LearningStructureValidatorTests
{
    private readonly CreateLearningStructureValidator _createValidator = new();
    private readonly UpdateLearningStructureValidator _updateValidator = new();
    private readonly SupersedeStructureValidator _supersedeValidator = new();

    #region CreateLearningStructureValidator Tests

    [Fact]
    public void CreateValidator_Should_Succeed_When_AllRequiredFieldsAreValid()
    {
        // Arrange
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: "Standard curriculum framework",
            AcademicPeriodId: Guid.NewGuid(),
            Version: "2026.1",
            EffectiveDate: new DateOnly(2026, 7, 1),
            EndDate: new DateOnly(2027, 6, 30),
            Notes: "Initial draft release");

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
    public void CreateValidator_Should_Fail_When_NameIsNullOrEmpty(string? name)
    {
        // Arrange
        var request = new CreateLearningStructureRequest(
            Name: name!,
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: "2026.1",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.Name)
            && e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_NameExceedsMaximumLength()
    {
        // Arrange
        var longName = new string('A', 201);
        var request = new CreateLearningStructureRequest(
            Name: longName,
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: "2026.1",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.Name)
            && e.ErrorMessage.Contains("200 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_AcademicPeriodIdIsEmpty()
    {
        // Arrange
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: null,
            AcademicPeriodId: Guid.Empty,
            Version: "2026.1",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.AcademicPeriodId)
            && e.ErrorMessage.Contains("Academic period ID is required", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CreateValidator_Should_Fail_When_VersionIsNullOrEmpty(string? version)
    {
        // Arrange
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: version!,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.Version)
            && e.ErrorMessage.Contains("Version is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_VersionExceedsMaximumLength()
    {
        // Arrange
        var longVersion = new string('1', 51);
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: longVersion,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.Version)
            && e.ErrorMessage.Contains("50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("2026.1")]
    [InlineData("v1.0.0")]
    [InlineData("2026/2027-REV1")]
    [InlineData("2026-Semester1")]
    [InlineData("1.0")]
    public void CreateValidator_Should_Accept_ValidVersionFormats(string versionFormat)
    {
        // Arrange
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: versionFormat,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_EffectiveDateIsDefault()
    {
        // Arrange
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: "2026.1",
            EffectiveDate: default);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.EffectiveDate)
            && e.ErrorMessage.Contains("Effective date is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_EndDateIsEarlierThanEffectiveDate()
    {
        // Arrange
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: "2026.1",
            EffectiveDate: new DateOnly(2026, 7, 1),
            EndDate: new DateOnly(2026, 6, 30));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.EndDate)
            && e.ErrorMessage.Contains("greater than or equal to effective date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_EndDateEqualsEffectiveDate()
    {
        // Arrange
        var date = new DateOnly(2026, 7, 1);
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: "2026.1",
            EffectiveDate: date,
            EndDate: date);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_DescriptionExceeds4000Characters()
    {
        // Arrange
        var longDesc = new string('D', 4001);
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: longDesc,
            AcademicPeriodId: Guid.NewGuid(),
            Version: "2026.1",
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.Description)
            && e.ErrorMessage.Contains("4000 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('N', 4001);
        var request = new CreateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026",
            Description: null,
            AcademicPeriodId: Guid.NewGuid(),
            Version: "2026.1",
            EffectiveDate: new DateOnly(2026, 7, 1),
            Notes: longNotes);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLearningStructureRequest.Notes)
            && e.ErrorMessage.Contains("4000 characters", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region UpdateLearningStructureValidator Tests

    [Fact]
    public void UpdateValidator_Should_Succeed_When_FieldsAreValid()
    {
        // Arrange
        var request = new UpdateLearningStructureRequest(
            Name: "Kurikulum Merdeka 2026 Update",
            Description: "Updated description",
            Notes: "Reviewed notes");

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
        var request = new UpdateLearningStructureRequest(Name: name!, Description: null, Notes: null);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningStructureRequest.Name)
            && e.ErrorMessage.Contains("Name is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_NameExceedsMaximumLength()
    {
        // Arrange
        var longName = new string('B', 201);
        var request = new UpdateLearningStructureRequest(Name: longName, Description: null, Notes: null);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningStructureRequest.Name)
            && e.ErrorMessage.Contains("200 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_DescriptionExceeds4000Characters()
    {
        // Arrange
        var longDesc = new string('D', 4001);
        var request = new UpdateLearningStructureRequest(Name: "Valid Name", Description: longDesc, Notes: null);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningStructureRequest.Description));
    }

    [Fact]
    public void UpdateValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var longNotes = new string('N', 4001);
        var request = new UpdateLearningStructureRequest(Name: "Valid Name", Description: null, Notes: longNotes);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLearningStructureRequest.Notes));
    }

    #endregion

    #region SupersedeStructureValidator Tests

    [Fact]
    public void SupersedeValidator_Should_Succeed_When_AllFieldsAreValid()
    {
        // Arrange
        var request = new SupersedeStructureRequest(
            NewVersion: "2026.2",
            EffectiveDate: new DateOnly(2026, 8, 1),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027 Ganjil");

        // Act
        var result = _supersedeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void SupersedeValidator_Should_Fail_When_NewVersionIsNullOrEmpty(string? newVersion)
    {
        // Arrange
        var request = new SupersedeStructureRequest(
            NewVersion: newVersion!,
            EffectiveDate: new DateOnly(2026, 8, 1));

        // Act
        var result = _supersedeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SupersedeStructureRequest.NewVersion)
            && e.ErrorMessage.Contains("New version string is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SupersedeValidator_Should_Fail_When_NewVersionExceeds50Characters()
    {
        // Arrange
        var longVersion = new string('V', 51);
        var request = new SupersedeStructureRequest(
            NewVersion: longVersion,
            EffectiveDate: new DateOnly(2026, 8, 1));

        // Act
        var result = _supersedeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SupersedeStructureRequest.NewVersion)
            && e.ErrorMessage.Contains("50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SupersedeValidator_Should_Fail_When_EffectiveDateIsDefault()
    {
        // Arrange
        var request = new SupersedeStructureRequest(
            NewVersion: "2026.2",
            EffectiveDate: default);

        // Act
        var result = _supersedeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SupersedeStructureRequest.EffectiveDate)
            && e.ErrorMessage.Contains("Effective date is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SupersedeValidator_Should_Fail_When_AcademicPeriodLabelExceeds100Characters()
    {
        // Arrange
        var longLabel = new string('L', 101);
        var request = new SupersedeStructureRequest(
            NewVersion: "2026.2",
            EffectiveDate: new DateOnly(2026, 8, 1),
            AcademicPeriodLabel: longLabel);

        // Act
        var result = _supersedeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SupersedeStructureRequest.AcademicPeriodLabel)
            && e.ErrorMessage.Contains("100 characters", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
