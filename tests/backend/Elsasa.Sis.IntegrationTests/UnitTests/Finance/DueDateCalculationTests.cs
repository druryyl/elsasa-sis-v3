namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

public sealed class DueDateCalculationTests
{
    [Theory]
    [InlineData("7", 7)]
    [InlineData("14", 14)]
    [InlineData("30", 30)]
    [InlineData("60", 60)]
    public void CalculateDueDate_DaysAfterIssuance_ShouldAddExactDays(string daysValue, int expectedDays)
    {
        // Arrange
        var issueDate = new DateOnly(2026, 11, 4);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.DaysAfterIssuance,
            daysValue,
            issueDate);

        // Assert
        Assert.Equal(issueDate.AddDays(expectedDays), result);
    }

    [Fact]
    public void CalculateDueDate_DaysAfterIssuance_InvalidValue_ShouldFallbackTo14Days()
    {
        // Arrange
        var issueDate = new DateOnly(2026, 11, 4);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.DaysAfterIssuance,
            "not-a-number",
            issueDate);

        // Assert
        Assert.Equal(issueDate.AddDays(14), result);
    }

    [Fact]
    public void CalculateDueDate_FixedDayOfMonth_StandardPlacement_ShouldSetExpectedDay()
    {
        // Arrange
        var issueDate = new DateOnly(2026, 11, 4);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            "10",
            issueDate);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 10), result);
    }

    [Fact]
    public void CalculateDueDate_FixedDayOfMonth_WithPeriodReferenceDate_ShouldUseReferenceMonth()
    {
        // Arrange
        var issueDate = new DateOnly(2026, 10, 25);
        var periodRefDate = new DateOnly(2026, 11, 1);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            "15",
            issueDate,
            periodReferenceDate: periodRefDate);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 15), result);
    }

    [Fact]
    public void CalculateDueDate_FixedDayOfMonth_31DayMonth_ShouldNotClampDay31()
    {
        // Arrange (October has 31 days)
        var issueDate = new DateOnly(2026, 10, 1);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            "31",
            issueDate);

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 31), result);
    }

    [Fact]
    public void CalculateDueDate_FixedDayOfMonth_30DayMonth_ShouldClampDay31To30()
    {
        // Arrange (November has 30 days)
        var issueDate = new DateOnly(2026, 11, 1);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            "31",
            issueDate);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 30), result);
    }

    [Fact]
    public void CalculateDueDate_FixedDayOfMonth_NonLeapFebruary_ShouldClampTo28()
    {
        // Arrange (February 2026 is non-leap year, has 28 days)
        var issueDate = new DateOnly(2026, 2, 1);

        // Act
        var result30 = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            "30",
            issueDate);

        var result31 = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            "31",
            issueDate);

        // Assert
        Assert.Equal(new DateOnly(2026, 2, 28), result30);
        Assert.Equal(new DateOnly(2026, 2, 28), result31);
    }

    [Fact]
    public void CalculateDueDate_FixedDayOfMonth_LeapYearFebruary_ShouldClampTo29()
    {
        // Arrange (February 2028 is a leap year, has 29 days)
        var issueDate = new DateOnly(2028, 2, 1);

        // Act
        var result29 = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            "29",
            issueDate);

        var result31 = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            "31",
            issueDate);

        // Assert
        Assert.Equal(new DateOnly(2028, 2, 29), result29);
        Assert.Equal(new DateOnly(2028, 2, 29), result31);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void CalculateDueDate_FixedDayOfMonth_DayLessThanOne_ShouldClampTo1(string invalidDay)
    {
        // Arrange
        var issueDate = new DateOnly(2026, 11, 15);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedDayOfMonth,
            invalidDay,
            issueDate);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 1), result);
    }

    [Fact]
    public void CalculateDueDate_FixedCalendarDate_ValidIsoDate_ShouldParseExactDate()
    {
        // Arrange
        var issueDate = new DateOnly(2026, 11, 4);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedCalendarDate,
            "2026-12-25",
            issueDate);

        // Assert
        Assert.Equal(new DateOnly(2026, 12, 25), result);
    }

    [Fact]
    public void CalculateDueDate_FixedCalendarDate_InvalidString_ShouldFallbackTo30DaysAfterIssuance()
    {
        // Arrange
        var issueDate = new DateOnly(2026, 11, 4);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            DueDateCalculationEngine.FixedCalendarDate,
            "invalid-date-string",
            issueDate);

        // Assert
        Assert.Equal(issueDate.AddDays(30), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown_strategy")]
    public void CalculateDueDate_UnknownOrEmptyStrategy_ShouldFallbackTo14Days(string? strategy)
    {
        // Arrange
        var issueDate = new DateOnly(2026, 11, 4);

        // Act
        var result = DueDateCalculationEngine.CalculateDueDate(
            strategy!,
            "10",
            issueDate);

        // Assert
        Assert.Equal(issueDate.AddDays(14), result);
    }
}
