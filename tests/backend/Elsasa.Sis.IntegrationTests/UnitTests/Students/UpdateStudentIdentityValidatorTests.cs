namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Records.UpdateStudentIdentity;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class UpdateStudentIdentityValidatorTests
{
    private readonly UpdateStudentIdentityValidator _validator = new();

    [Fact]
    public void Validate_Should_Succeed_When_MinimalValidPayloadProvided()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Ahmad Rizky");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Succeed_When_AllValidFieldsProvided()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Ahmad Rizky Pratama",
            PreferredName: "Rizky",
            DateOfBirth: new DateOnly(2010, 5, 15),
            PlaceOfBirth: "Jakarta",
            Gender: "male",
            Nationality: "Indonesian",
            GovernmentIdType: "NIK",
            GovernmentIdNumber: "3171012345670001",
            Address: "Jl. Sudirman No. 45, Jakarta Selatan",
            Phone: "+6281234567890",
            Email: "rizky.pratama@example.com",
            IsIndependent: true,
            Notes: "Enrolled with certified documentation");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_FullNameIsEmptyOrWhitespace(string? fullName)
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: fullName!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_FullNameExceeds200Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: new string('A', 201));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_Should_Succeed_When_FullNameIsExactly200Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: new string('A', 200));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Validate_Should_Fail_When_PreferredNameExceeds100Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            PreferredName: new string('B', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PreferredName)
            .WithErrorMessage("Preferred name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_DateOfBirthIsInTheFuture()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            DateOfBirth: futureDate);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("Date of birth cannot be in the future.");
    }

    [Fact]
    public void Validate_Should_Succeed_When_DateOfBirthIsTodayOrInPast()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            DateOfBirth: today);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validate_Should_Fail_When_PlaceOfBirthExceeds100Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            PlaceOfBirth: new string('C', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PlaceOfBirth)
            .WithErrorMessage("Place of birth cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_GenderExceeds20Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            Gender: new string('D', 21));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender)
            .WithErrorMessage("Gender cannot exceed 20 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_NationalityExceeds100Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            Nationality: new string('E', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Nationality)
            .WithErrorMessage("Nationality cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_GovernmentIdTypeExceeds32Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            GovernmentIdType: new string('F', 33));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GovernmentIdType)
            .WithErrorMessage("Government ID type cannot exceed 32 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_GovernmentIdNumberExceeds64Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            GovernmentIdNumber: new string('1', 65));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GovernmentIdNumber)
            .WithErrorMessage("Government ID number cannot exceed 64 characters.");
    }

    [Fact]
    public void Validate_Should_Fail_When_PhoneExceeds32Characters()
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            Phone: new string('9', 33));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone cannot exceed 32 characters.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    [InlineData("plainaddress")]
    public void Validate_Should_Fail_When_EmailFormatIsInvalid(string invalidEmail)
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            Email: invalidEmail);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailExceeds254Characters()
    {
        // Arrange - e.g. 245 'a's + "@domain.com" = 256 chars
        var longEmail = $"{new string('a', 245)}@domain.com";
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            Email: longEmail);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email cannot exceed 254 characters.");
    }

    [Theory]
    [InlineData("student@example.com")]
    [InlineData("first.last@school.edu.id")]
    [InlineData("student123@subdomain.example.org")]
    public void Validate_Should_Succeed_When_EmailIsValidFormat(string validEmail)
    {
        // Arrange
        var request = new UpdateStudentIdentityRequest(
            FullName: "Valid Name",
            Email: validEmail);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}
