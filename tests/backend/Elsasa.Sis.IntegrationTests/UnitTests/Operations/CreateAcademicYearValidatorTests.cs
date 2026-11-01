namespace Elsasa.Sis.IntegrationTests.UnitTests.Operations;

using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CreateAcademicYearValidator"/> ensuring 100% rule coverage (ADR-005).
/// </summary>
public sealed class CreateAcademicYearValidatorTests
{
    private readonly CreateAcademicYearValidator _validator = new();

    [Fact]
    public void Validate_Should_Succeed_When_AllFieldsAreValid()
    {
        // Arrange
        var request = new CreateAcademicYearRequest(
            Code: "AY-2026-2027",
            Name: "Academic Year 2026/2027",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Validate_Should_Fail_When_CodeIsNullOrEmpty(string? code)
    {
        // Arrange
        var request = new CreateAcademicYearRequest(
            Code: code!,
            Name: "Academic Year 2026/2027",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicYearRequest.Code));
    }

    [Fact]
    public void Validate_Should_Fail_When_CodeExceedsMaximumLength()
    {
        // Arrange
        var longCode = new string('A', 33);
        var request = new CreateAcademicYearRequest(
            Code: longCode,
            Name: "Academic Year 2026/2027",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateAcademicYearRequest.Code)
            && e.ErrorMessage.Contains("32 characters", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Validate_Should_Fail_When_NameIsNullOrEmpty(string? name)
    {
        // Arrange
        var request = new CreateAcademicYearRequest(
            Code: "AY-2026-2027",
            Name: name!,
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicYearRequest.Name));
    }

    [Fact]
    public void Validate_Should_Fail_When_NameExceedsMaximumLength()
    {
        // Arrange
        var longName = new string('A', 201);
        var request = new CreateAcademicYearRequest(
            Code: "AY-2026-2027",
            Name: longName,
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2027, 6, 30));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateAcademicYearRequest.Name)
            && e.ErrorMessage.Contains("200 characters", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Should_Fail_When_StartDateIsDefault()
    {
        // Arrange
        var request = new CreateAcademicYearRequest(
            Code: "AY-2026-2027",
            Name: "Academic Year 2026/2027",
            StartDate: default,
            EndDate: new DateOnly(2027, 6, 30));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicYearRequest.StartDate));
    }

    [Fact]
    public void Validate_Should_Fail_When_EndDateIsDefault()
    {
        // Arrange
        var request = new CreateAcademicYearRequest(
            Code: "AY-2026-2027",
            Name: "Academic Year 2026/2027",
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: default);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAcademicYearRequest.EndDate));
    }

    [Fact]
    public void Validate_Should_Fail_When_EndDateIsEqualToStartDate()
    {
        // Arrange
        var sameDate = new DateOnly(2026, 9, 1);
        var request = new CreateAcademicYearRequest(
            Code: "AY-2026-2027",
            Name: "Academic Year 2026/2027",
            StartDate: sameDate,
            EndDate: sameDate);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateAcademicYearRequest.EndDate)
            && e.ErrorMessage.Contains("greater than start date", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Should_Fail_When_EndDateIsEarlierThanStartDate()
    {
        // Arrange
        var request = new CreateAcademicYearRequest(
            Code: "AY-2026-2027",
            Name: "Academic Year 2026/2027",
            StartDate: new DateOnly(2027, 6, 30),
            EndDate: new DateOnly(2026, 9, 1));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateAcademicYearRequest.EndDate)
            && e.ErrorMessage.Contains("greater than start date", StringComparison.Ordinal));
    }
}
