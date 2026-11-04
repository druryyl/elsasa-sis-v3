namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Services;
using System.Text.RegularExpressions;

/// <summary>
/// Unit tests for finance canonical number generators and Indonesian words converter (TD-03, TD-04, TD-05).
/// Verifies patterns: PAY-{Year}-{Sequence:D6}, REC-{Year}-{Sequence:D6}, ALC-{Year}-{Sequence:D6}, and PYR-{Year}-{Sequence:D4}.
/// </summary>
public sealed class NumberGeneratorTests
{
    private static readonly Regex _paymentPattern = new(@"^PAY-\d{4}-\d{6}$", RegexOptions.Compiled);
    private static readonly Regex _receiptPattern = new(@"^REC-\d{4}-\d{6}$", RegexOptions.Compiled);
    private static readonly Regex _allocationPattern = new(@"^ALC-\d{4}-\d{6}$", RegexOptions.Compiled);
    private static readonly Regex _payerPattern = new(@"^PYR-\d{4}-\d{4}$", RegexOptions.Compiled);

    #region PaymentNumberGenerator Tests

    [Fact]
    public void PaymentNumberGenerator_Prefix_ShouldBePAY()
    {
        Assert.Equal("PAY", PaymentNumberGenerator.Prefix);
    }

    [Theory]
    [InlineData(1, 2026, "PAY-2026-000001")]
    [InlineData(42, 2026, "PAY-2026-000042")]
    [InlineData(12345, 2026, "PAY-2026-012345")]
    [InlineData(999999, 2027, "PAY-2027-999999")]
    public void PaymentNumberGenerator_Format_WithExplicitYear_ShouldMatchPattern(int sequence, int year, string expected)
    {
        // Act
        var formatted = PaymentNumberGenerator.Format(sequence, year);

        // Assert
        Assert.Equal(expected, formatted);
        Assert.Matches(_paymentPattern, formatted);
    }

    [Fact]
    public void PaymentNumberGenerator_Format_WithDefaultYear_ShouldUseCurrentUtcYear()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;

        // Act
        var formatted = PaymentNumberGenerator.Format(100);

        // Assert
        Assert.Equal($"PAY-{currentYear}-000100", formatted);
        Assert.Matches(_paymentPattern, formatted);
    }

    #endregion

    #region ReceiptNumberGenerator Tests

    [Fact]
    public void ReceiptNumberGenerator_Prefix_ShouldBeREC()
    {
        Assert.Equal("REC", ReceiptNumberGenerator.Prefix);
    }

    [Theory]
    [InlineData(1, 2026, "REC-2026-000001")]
    [InlineData(99, 2026, "REC-2026-000099")]
    [InlineData(876543, 2026, "REC-2026-876543")]
    [InlineData(999999, 2028, "REC-2028-999999")]
    public void ReceiptNumberGenerator_Format_WithExplicitYear_ShouldMatchPattern(int sequence, int year, string expected)
    {
        // Act
        var formatted = ReceiptNumberGenerator.Format(sequence, year);

        // Assert
        Assert.Equal(expected, formatted);
        Assert.Matches(_receiptPattern, formatted);
    }

    [Fact]
    public void ReceiptNumberGenerator_Format_WithDefaultYear_ShouldUseCurrentUtcYear()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;

        // Act
        var formatted = ReceiptNumberGenerator.Format(1);

        // Assert
        Assert.Equal($"REC-{currentYear}-000001", formatted);
        Assert.Matches(_receiptPattern, formatted);
    }

    #endregion

    #region AllocationNumberGenerator Tests

    [Fact]
    public void AllocationNumberGenerator_Prefix_ShouldBeALC()
    {
        Assert.Equal("ALC", AllocationNumberGenerator.Prefix);
    }

    [Theory]
    [InlineData(1, 2026, "ALC-2026-000001")]
    [InlineData(7, 2026, "ALC-2026-000007")]
    [InlineData(1234, 2026, "ALC-2026-001234")]
    [InlineData(50000, 2025, "ALC-2025-050000")]
    public void AllocationNumberGenerator_Format_WithExplicitYear_ShouldMatchPattern(int sequence, int year, string expected)
    {
        // Act
        var formatted = AllocationNumberGenerator.Format(sequence, year);

        // Assert
        Assert.Equal(expected, formatted);
        Assert.Matches(_allocationPattern, formatted);
    }

    [Fact]
    public void AllocationNumberGenerator_Format_WithDefaultYear_ShouldUseCurrentUtcYear()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;

        // Act
        var formatted = AllocationNumberGenerator.Format(55);

        // Assert
        Assert.Equal($"ALC-{currentYear}-000055", formatted);
        Assert.Matches(_allocationPattern, formatted);
    }

    #endregion

    #region PayerNumberGenerator Tests

    [Fact]
    public void PayerNumberGenerator_Prefix_ShouldBePYR()
    {
        Assert.Equal("PYR", PayerNumberGenerator.Prefix);
    }

    [Theory]
    [InlineData(1, 2026, "PYR-2026-0001")]
    [InlineData(42, 2026, "PYR-2026-0042")]
    [InlineData(500, 2026, "PYR-2026-0500")]
    [InlineData(9999, 2026, "PYR-2026-9999")]
    public void PayerNumberGenerator_Format_WithExplicitYear_ShouldMatchPattern(int sequence, int year, string expected)
    {
        // Act
        var formatted = PayerNumberGenerator.Format(sequence, year);

        // Assert
        Assert.Equal(expected, formatted);
        Assert.Matches(_payerPattern, formatted);
    }

    [Fact]
    public void PayerNumberGenerator_Format_WithDefaultYear_ShouldUseCurrentUtcYear()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;

        // Act
        var formatted = PayerNumberGenerator.Format(1);

        // Assert
        Assert.Equal($"PYR-{currentYear}-0001", formatted);
        Assert.Matches(_payerPattern, formatted);
    }

    #endregion

    #region NumberToWordsConverter Tests

    [Theory]
    [InlineData(0, "Nol Rupiah")]
    [InlineData(1, "Satu Rupiah")]
    [InlineData(11, "Sebelas Rupiah")]
    [InlineData(15, "Lima Belas Rupiah")]
    [InlineData(100, "Seratus Rupiah")]
    [InlineData(150, "Seratus Lima Puluh Rupiah")]
    [InlineData(1000, "Seribu Rupiah")]
    [InlineData(1500, "Seribu Lima Ratus Rupiah")]
    [InlineData(150_000, "Seratus Lima Puluh Ribu Rupiah")]
    [InlineData(1_000_000, "Satu Juta Rupiah")]
    [InlineData(2_500_000, "Dua Juta Lima Ratus Ribu Rupiah")]
    [InlineData(10_750_000, "Sepuluh Juta Tujuh Ratus Lima Puluh Ribu Rupiah")]
    [InlineData(-500_000, "Minus Lima Ratus Ribu Rupiah")]
    public void NumberToWordsConverter_ToIndonesianWords_ShouldConvertCorrectly(long amount, string expected)
    {
        // Act
        var result = NumberToWordsConverter.ToIndonesianWords(amount);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion
}
