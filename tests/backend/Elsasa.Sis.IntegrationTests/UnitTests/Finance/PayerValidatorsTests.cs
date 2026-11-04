namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Payers.CreatePayer;
using FluentValidation.TestHelper;

/// <summary>
/// Unit tests for <see cref="CreatePayerValidator"/> verifying input validation for payer creation (SCR-FIN-02, TD-03).
/// </summary>
public sealed class PayerValidatorsTests
{
    private readonly CreatePayerValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_WithAllFields_ShouldPassValidation()
    {
        // Arrange
        var request = new CreatePayerRequest(
            Name: "PT Global Mandiri Sponsor",
            Type: "sponsor",
            IdentificationNumber: "01.234.567.8-901.000",
            Email: "csr@globalmandiri.co.id",
            Phone: "+62 21 555 9876",
            BillingAddress: "Jl. Sudirman Kav. 45, Jakarta",
            BankAccountName: "PT Global Mandiri Sponsor",
            BankAccountNumber: "1234567890",
            BankName: "BCA",
            Notes: "Scholarship funding sponsor for 2026 cohort");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidRequest_WithMinimalFields_ShouldPassValidation()
    {
        // Arrange
        var request = new CreatePayerRequest(
            Name: "Budi Santoso",
            Type: null,
            IdentificationNumber: null,
            Email: null,
            Phone: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceName_ShouldFailValidation(string? name)
    {
        // Arrange
        var request = new CreatePayerRequest(
            Name: name!,
            Type: "parent_guardian",
            Email: "parent@example.com",
            Phone: "+628123456789");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Payer name is required.");
    }

    [Fact]
    public void Validate_NameExceeding200Chars_ShouldFailValidation()
    {
        // Arrange
        var name = new string('A', 201);
        var request = new CreatePayerRequest(Name: name);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Payer name must not exceed 200 characters.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("plainaddress")]
    [InlineData("@missingusername.com")]
    [InlineData("user@")]
    public void Validate_InvalidEmailFormat_ShouldFailValidation(string invalidEmail)
    {
        // Arrange
        var request = new CreatePayerRequest(
            Name: "Budi Santoso",
            Email: invalidEmail);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email address is not in a valid format.");
    }

    [Theory]
    [InlineData("sponsor@example.com")]
    [InlineData("budi.santoso@gmail.com")]
    [InlineData("csr-grant@foundations.org.id")]
    [InlineData("parent+student123@yahoo.co.id")]
    public void Validate_ValidEmailFormat_ShouldPassValidation(string validEmail)
    {
        // Arrange
        var request = new CreatePayerRequest(
            Name: "Budi Santoso",
            Email: validEmail);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmailExceeding200Chars_ShouldFailValidation()
    {
        // Arrange
        var longEmail = $"{new string('a', 190)}@example.com";
        var request = new CreatePayerRequest(
            Name: "Budi Santoso",
            Email: longEmail);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("+6281234567890")]
    [InlineData("081234567890")]
    [InlineData("(021) 555-1234")]
    [InlineData("+62 21 555 1234")]
    [InlineData("021-5551234")]
    [InlineData("+1-800-555-0199")]
    public void Validate_ValidPhoneFormats_ShouldPassValidation(string validPhone)
    {
        // Arrange
        var request = new CreatePayerRequest(
            Name: "Budi Santoso",
            Phone: validPhone);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456")]
    [InlineData("abc")]
    [InlineData("+62invalid!")]
    [InlineData("phone: 08123")]
    public void Validate_InvalidPhoneFormats_ShouldFailValidation(string invalidPhone)
    {
        // Arrange
        var request = new CreatePayerRequest(
            Name: "Budi Santoso",
            Phone: invalidPhone);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone number must contain at least 7 valid phone digits/symbols.");
    }

    [Fact]
    public void Validate_PhoneExceeding30Chars_ShouldFailValidation()
    {
        // Arrange
        var longPhone = "+62" + new string('9', 30);
        var request = new CreatePayerRequest(
            Name: "Budi Santoso",
            Phone: longPhone);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Validate_OptionalFieldsLengthLimits_ShouldFailWhenExceeded()
    {
        // Arrange
        var request = new CreatePayerRequest(
            Name: "Budi Santoso",
            Type: new string('T', 65),
            IdentificationNumber: new string('I', 65),
            BankAccountName: new string('B', 201),
            BankAccountNumber: new string('N', 101),
            BankName: new string('K', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Payer type must not exceed 64 characters.");
        result.ShouldHaveValidationErrorFor(x => x.IdentificationNumber)
            .WithErrorMessage("Identification number must not exceed 64 characters.");
        result.ShouldHaveValidationErrorFor(x => x.BankAccountName)
            .WithErrorMessage("Bank account name must not exceed 200 characters.");
        result.ShouldHaveValidationErrorFor(x => x.BankAccountNumber)
            .WithErrorMessage("Bank account number must not exceed 100 characters.");
        result.ShouldHaveValidationErrorFor(x => x.BankName)
            .WithErrorMessage("Bank name must not exceed 100 characters.");
    }
}
