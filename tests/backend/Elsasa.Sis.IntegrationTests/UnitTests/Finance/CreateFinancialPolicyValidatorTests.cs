namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;

public sealed class CreateFinancialPolicyValidatorTests
{
    private readonly CreateFinancialPolicyValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: "Standard Tuition Policy 2026/2027",
            Category: "tuition",
            BusinessPurpose: "Establish standard tuition fees and payment schedules for academic year 2026/2027.",
            ApplicableContext: "All campuses and programs",
            EffectiveDate: new DateOnly(2026, 7, 1),
            EndDate: new DateOnly(2027, 6, 30),
            PolicyVersion: "1.0",
            Notes: "Initial approval draft");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyPolicyCode_ShouldFailValidation(string? policyCode)
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: policyCode!,
            Name: "Test Policy",
            Category: "tuition",
            BusinessPurpose: "Test purpose",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.PolicyCode));
    }

    [Theory]
    [InlineData("INVALID-CODE")]
    [InlineData("FP-2026-1")]
    [InlineData("FP-26-001")]
    [InlineData("fp-2026-001")]
    [InlineData("POLICY-1234")]
    public void Validate_MalformedPolicyCode_ShouldFailValidation(string policyCode)
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: policyCode,
            Name: "Test Policy",
            Category: "tuition",
            BusinessPurpose: "Test purpose",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.PolicyCode));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyName_ShouldFailValidation(string? name)
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: name!,
            Category: "tuition",
            BusinessPurpose: "Test purpose",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Name));
    }

    [Fact]
    public void Validate_NameExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: new string('A', 201),
            Category: "tuition",
            BusinessPurpose: "Test purpose",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyCategory_ShouldFailValidation(string? category)
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: "Valid Policy Name",
            Category: category!,
            BusinessPurpose: "Test purpose",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Category));
    }

    [Fact]
    public void Validate_CategoryExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: "Valid Policy Name",
            Category: new string('C', 65),
            BusinessPurpose: "Test purpose",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Category));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyBusinessPurpose_ShouldFailValidation(string? purpose)
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: "Valid Policy Name",
            Category: "tuition",
            BusinessPurpose: purpose!,
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.BusinessPurpose));
    }

    [Fact]
    public void Validate_DefaultEffectiveDate_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: "Valid Policy Name",
            Category: "tuition",
            BusinessPurpose: "Test purpose",
            ApplicableContext: null,
            EffectiveDate: default);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.EffectiveDate));
    }

    [Fact]
    public void Validate_EndDateBeforeEffectiveDate_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: "Valid Policy Name",
            Category: "tuition",
            BusinessPurpose: "Test purpose",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1),
            EndDate: new DateOnly(2026, 6, 30)); // 1 day before effective date

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.EndDate));
    }

    [Fact]
    public void Validate_EndDateEqualToEffectiveDate_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: "Single Day Policy",
            Category: "special_event",
            BusinessPurpose: "One-day workshop policy",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1),
            EndDate: new DateOnly(2026, 7, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NullEndDate_ShouldPassValidation()
    {
        // Arrange (Indefinite policy)
        var request = new CreateFinancialPolicyRequest(
            PolicyCode: "FP-2026-001",
            Name: "Ongoing Tuition Policy",
            Category: "tuition",
            BusinessPurpose: "Indefinite policy purpose",
            ApplicableContext: null,
            EffectiveDate: new DateOnly(2026, 7, 1),
            EndDate: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
