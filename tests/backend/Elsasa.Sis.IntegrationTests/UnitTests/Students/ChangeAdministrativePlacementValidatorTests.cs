namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class ChangeAdministrativePlacementValidatorTests
{
    private readonly ChangeAdministrativePlacementValidator _validator = new();

    private static ChangeAdministrativePlacementRequest CreateValidRequest(
        string campus = "Main Campus",
        string schoolLevel = "High School",
        string grade = "Grade 10",
        string academicYear = "2026/2027",
        DateOnly? effectiveDate = null,
        string? program = "Standard",
        string? stream = "Science",
        string? cohort = "2026-A",
        string? homeroom = "Room 101",
        string? reason = "Routine advancement",
        string? authorizedBy = "admin-user",
        string? decisionReference = "DEC-2026-001")
    {
        return new ChangeAdministrativePlacementRequest(
            Campus: campus,
            SchoolLevel: schoolLevel,
            Grade: grade,
            AcademicYear: academicYear,
            EffectiveDate: effectiveDate ?? new DateOnly(2026, 9, 1),
            Program: program,
            Stream: stream,
            Cohort: cohort,
            Homeroom: homeroom,
            Reason: reason,
            AuthorizedBy: authorizedBy,
            DecisionReference: decisionReference);
    }

    [Fact]
    public void Validate_Should_Pass_When_AllRequiredAndOptionalFieldsAreValid()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_OnlyRequiredFieldsAreProvided()
    {
        // Arrange
        var request = CreateValidRequest(
            program: null,
            stream: null,
            cohort: null,
            homeroom: null,
            reason: null,
            authorizedBy: null,
            decisionReference: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_CampusIsEmptyOrWhitespace(string? campus)
    {
        // Arrange
        var request = CreateValidRequest(campus: campus!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Campus)
            .WithErrorMessage("Campus is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_CampusExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(campus: new string('a', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Campus)
            .WithErrorMessage("Campus cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_SchoolLevelIsEmptyOrWhitespace(string? schoolLevel)
    {
        // Arrange
        var request = CreateValidRequest(schoolLevel: schoolLevel!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SchoolLevel)
            .WithErrorMessage("School level is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_SchoolLevelExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(schoolLevel: new string('a', 65));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SchoolLevel)
            .WithErrorMessage("School level cannot exceed 64 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_GradeIsEmptyOrWhitespace(string? grade)
    {
        // Arrange
        var request = CreateValidRequest(grade: grade!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Grade)
            .WithErrorMessage("Grade is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_GradeExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(grade: new string('a', 65));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Grade)
            .WithErrorMessage("Grade cannot exceed 64 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_AcademicYearIsEmptyOrWhitespace(string? academicYear)
    {
        // Arrange
        var request = CreateValidRequest(academicYear: academicYear!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AcademicYear)
            .WithErrorMessage("Academic year is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_AcademicYearExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(academicYear: new string('a', 33));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AcademicYear)
            .WithErrorMessage("Academic year cannot exceed 32 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_EffectiveDateIsDefault()
    {
        // Arrange
        var request = CreateValidRequest(effectiveDate: default(DateOnly));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Fact]
    public void Validate_Should_Fail_When_ProgramExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(program: new string('a', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Program)
            .WithErrorMessage("Program cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_StreamExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(stream: new string('a', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Stream)
            .WithErrorMessage("Stream cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_CohortExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(cohort: new string('a', 51));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Cohort)
            .WithErrorMessage("Cohort cannot exceed 50 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_HomeroomExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(homeroom: new string('a', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Homeroom)
            .WithErrorMessage("Homeroom cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_ReasonExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(reason: new string('a', 501));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_AuthorizedByExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(authorizedBy: new string('a', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AuthorizedBy)
            .WithErrorMessage("Authorized by cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_DecisionReferenceExceedsMaxLength()
    {
        // Arrange
        var request = CreateValidRequest(decisionReference: new string('a', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DecisionReference)
            .WithErrorMessage("Decision reference cannot exceed 100 characters.");
    }
}
