namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;

public sealed class CreateChargeRuleValidatorTests
{
    private readonly CreateChargeRuleValidator _validator = new();

    [Fact]
    public void Validate_ValidDaysAfterIssuanceRule_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-APP-001",
            Name: "Biaya Pendaftaran Siswa Baru",
            ChargeType: "application_fee",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 250000,
            CurrencyCode: "IDR",
            RecurrencePattern: "one_time",
            DueRuleType: "days_after_issuance",
            DueRuleValue: "14",
            ApplicableGrade: null,
            ApplicableProgram: null,
            Notes: "Registration fee for new applicants");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidFixedDayOfMonthRule_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-SPP-SD",
            Name: "SPP Bulanan SD",
            ChargeType: "tuition",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 750000,
            CurrencyCode: "IDR",
            RecurrencePattern: "monthly",
            DueRuleType: "fixed_day_of_month",
            DueRuleValue: "10",
            ApplicableGrade: null,
            ApplicableProgram: "SD Reguler");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidFixedCalendarDateRule_ShouldPassValidation()
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-ENROLL-001",
            Name: "Uang Pangkal Masuk",
            ChargeType: "enrollment_fee",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 15000000,
            CurrencyCode: "IDR",
            RecurrencePattern: "annual",
            DueRuleType: "fixed_calendar_date",
            DueRuleValue: "2026-08-31",
            ApplicableGrade: null,
            ApplicableProgram: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Validate_NonPositiveBaseAmount_ShouldFailValidation(long amount)
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-TEST",
            Name: "Test Charge Rule",
            ChargeType: "tuition",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: amount,
            CurrencyCode: "IDR",
            RecurrencePattern: "monthly",
            DueRuleType: "fixed_day_of_month",
            DueRuleValue: "10");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.BaseAmount));
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("")]
    public void Validate_NonIdrCurrency_ShouldFailValidation(string currency)
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-TEST",
            Name: "Test Charge Rule",
            ChargeType: "tuition",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 100000,
            CurrencyCode: currency,
            RecurrencePattern: "monthly",
            DueRuleType: "fixed_day_of_month",
            DueRuleValue: "10");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.CurrencyCode));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyRecurrencePattern_ShouldFailValidation(string? pattern)
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-TEST",
            Name: "Test Charge Rule",
            ChargeType: "tuition",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 100000,
            CurrencyCode: "IDR",
            RecurrencePattern: pattern!,
            DueRuleType: "fixed_day_of_month",
            DueRuleValue: "10");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.RecurrencePattern));
    }

    [Theory]
    [InlineData("unsupported_rule_type")]
    [InlineData("random_string")]
    [InlineData("")]
    public void Validate_InvalidDueRuleType_ShouldFailValidation(string ruleType)
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-TEST",
            Name: "Test Charge Rule",
            ChargeType: "tuition",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 100000,
            CurrencyCode: "IDR",
            RecurrencePattern: "monthly",
            DueRuleType: ruleType,
            DueRuleValue: "10");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.DueRuleType));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("not_a_number")]
    public void Validate_InvalidDaysAfterIssuanceValue_ShouldFailValidation(string invalidValue)
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-TEST",
            Name: "Test Charge Rule",
            ChargeType: "tuition",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 100000,
            CurrencyCode: "IDR",
            RecurrencePattern: "one_time",
            DueRuleType: "days_after_issuance",
            DueRuleValue: invalidValue);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.DueRuleValue));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("32")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void Validate_InvalidFixedDayOfMonthValue_ShouldFailValidation(string invalidValue)
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-TEST",
            Name: "Test Charge Rule",
            ChargeType: "tuition",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 100000,
            CurrencyCode: "IDR",
            RecurrencePattern: "monthly",
            DueRuleType: "fixed_day_of_month",
            DueRuleValue: invalidValue);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.DueRuleValue));
    }

    [Theory]
    [InlineData("invalid-date")]
    [InlineData("not-a-date")]
    [InlineData("2026-13-45")]
    public void Validate_InvalidFixedCalendarDateValue_ShouldFailValidation(string invalidValue)
    {
        // Arrange
        var request = new CreateChargeRuleRequest(
            RuleCode: "CR-2026-TEST",
            Name: "Test Charge Rule",
            ChargeType: "enrollment_fee",
            IsMandatory: true,
            DeterminationMethod: "fixed_schedule",
            BaseAmount: 100000,
            CurrencyCode: "IDR",
            RecurrencePattern: "annual",
            DueRuleType: "fixed_calendar_date",
            DueRuleValue: invalidValue);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.DueRuleValue));
    }
}
