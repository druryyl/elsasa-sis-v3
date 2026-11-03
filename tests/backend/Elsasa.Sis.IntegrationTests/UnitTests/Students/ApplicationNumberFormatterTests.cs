namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using System.Globalization;
using Elsasa.Sis.Students.Features.Admission.Common;
using FluentAssertions;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class ApplicationNumberFormatterTests
{
    [Theory]
    [InlineData(2026, 1, "ADM-2026-0001")]
    [InlineData(2026, 2, "ADM-2026-0002")]
    [InlineData(2026, 42, "ADM-2026-0042")]
    [InlineData(2026, 100, "ADM-2026-0100")]
    [InlineData(2026, 999, "ADM-2026-0999")]
    [InlineData(2026, 1000, "ADM-2026-1000")]
    [InlineData(2026, 9999, "ADM-2026-9999")]
    [InlineData(2026, 10000, "ADM-2026-10000")]
    [InlineData(2025, 1, "ADM-2025-0001")]
    [InlineData(2030, 500, "ADM-2030-0500")]
    public void Format_WithIntYear_Should_ReturnExpectedFormat(int year, int sequence, string expected)
    {
        // Act
        var result = ApplicationNumberFormatter.Format(year, sequence);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("2026", 1, "ADM-2026-0001")]
    [InlineData("2026", 9999, "ADM-2026-9999")]
    [InlineData(" 2026 ", 1, "ADM-2026-0001")]
    [InlineData("2025-2026", 5, "ADM-2025-2026-0005")]
    public void Format_WithStringYearCode_Should_ReturnExpectedFormat(string yearCode, int sequence, string expected)
    {
        // Act
        var result = ApplicationNumberFormatter.Format(yearCode, sequence);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_WithStringYearCode_Should_ThrowArgumentException_When_YearCodeIsNullOrWhiteSpace(string? invalidYearCode)
    {
        // Act
        var act = () => ApplicationNumberFormatter.Format(invalidYearCode!, 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Format_Should_BeCultureInvariant_AcrossDifferentCultures()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            // Set culture with non-standard digit substitution or formatting if present (e.g. ar-SA)
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");

            // Act
            var result = ApplicationNumberFormatter.Format(2026, 1);

            // Assert - Must remain standard ASCII digits ADM-2026-0001
            result.Should().Be("ADM-2026-0001");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
