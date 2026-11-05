namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for session attendance 24-hour temporal grace window domain logic (P6-S14, Criterion 8, GAP-005, OQ-003, TD-04, UC-ACD-03).
/// Verifies acceptance within 24 hours of activity creation or when activity date is same-day/future,
/// and rejection when activity is in the past and created beyond 24 hours.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class AttendanceGraceWindowTests
{
    private static readonly DateTimeOffset _referenceNow = new(2026, 11, 10, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _today = DateOnly.FromDateTime(_referenceNow.DateTime); // 2026-11-10

    [Fact]
    public void GraceWindow_Should_Accept_When_ActivityDateIsToday_RegardlessOfCreatedAtAge()
    {
        // Arrange - activity is today, but entity was created 30 days ago
        var activityDate = _today;
        var createdAt = _referenceNow.AddDays(-30);

        // Act
        var isWithinWindow = AttendanceGraceWindow.IsWithinWindow(activityDate, createdAt, _referenceNow);

        // Assert
        isWithinWindow.Should().BeTrue("same-day roll-call recording is always permitted within current date");
    }

    [Fact]
    public void GraceWindow_Should_Accept_When_ActivityDateIsInFuture()
    {
        // Arrange - future scheduled activity
        var activityDate = _today.AddDays(7);
        var createdAt = _referenceNow.AddDays(-5);

        // Act
        var isWithinWindow = AttendanceGraceWindow.IsWithinWindow(activityDate, createdAt, _referenceNow);

        // Assert
        isWithinWindow.Should().BeTrue("future activities are always within the planning/attendance window");
    }

    [Theory]
    [InlineData(0)]        // Created right now
    [InlineData(1)]        // Created 1 hour ago
    [InlineData(6)]        // Created 6 hours ago
    [InlineData(12)]       // Created 12 hours ago
    [InlineData(23)]       // Created 23 hours ago
    public void GraceWindow_Should_Accept_When_PastActivityWasCreatedWithin24Hours(int hoursAgo)
    {
        // Arrange - activity occurred yesterday, but was created recently (< 24 hours ago)
        var activityDate = _today.AddDays(-1);
        var createdAt = _referenceNow.AddHours(-hoursAgo);

        // Act
        var isWithinWindow = AttendanceGraceWindow.IsWithinWindow(activityDate, createdAt, _referenceNow);

        // Assert
        isWithinWindow.Should().BeTrue($"activity created {hoursAgo} hours ago is within the 24-hour grace window");
    }

    [Fact]
    public void GraceWindow_Should_Accept_At_ExactBoundaryOf24Hours()
    {
        // Arrange - exactly 24 hours ago
        var activityDate = _today.AddDays(-1);
        var createdAt = _referenceNow.Subtract(TimeSpan.FromHours(24));

        // Act
        var isWithinWindow = AttendanceGraceWindow.IsWithinWindow(activityDate, createdAt, _referenceNow);

        // Assert
        isWithinWindow.Should().BeTrue("exact 24-hour boundary is inclusive");
    }

    [Theory]
    [InlineData(1)]          // 24 hours and 1 second ago
    [InlineData(60)]         // 24 hours and 1 minute ago
    [InlineData(3600)]       // 25 hours ago
    [InlineData(86400)]      // 48 hours ago
    [InlineData(86400 * 7)]  // 8 days ago
    public void GraceWindow_Should_Reject_When_PastActivityWasCreatedBeyond24Hours(int extraSeconds)
    {
        // Arrange - activity occurred in the past and was created more than 24 hours ago
        var activityDate = _today.AddDays(-1);
        var createdAt = _referenceNow.Subtract(TimeSpan.FromHours(24)).AddSeconds(-extraSeconds);

        // Act
        var isWithinWindow = AttendanceGraceWindow.IsWithinWindow(activityDate, createdAt, _referenceNow);

        // Assert
        isWithinWindow.Should().BeFalse($"activity created {24 * 3600 + extraSeconds} seconds ago must be rejected as grace window expired");
    }

    [Fact]
    public void GraceDuration_Should_BeConfiguredTo24Hours()
    {
        // Assert
        AttendanceGraceWindow.GraceDuration.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void GraceWindow_Should_HandleDifferentTimezonesConsistently()
    {
        // Arrange - now is 15:00 UTC+7 (which is 08:00 UTC)
        var nowWithOffset = new DateTimeOffset(2026, 11, 10, 15, 0, 0, TimeSpan.FromHours(7));
        var activityDate = DateOnly.FromDateTime(nowWithOffset.DateTime); // 2026-11-10
        var createdAtUtc = nowWithOffset.ToUniversalTime().AddHours(-10);

        // Act
        var isWithinWindow = AttendanceGraceWindow.IsWithinWindow(activityDate, createdAtUtc, nowWithOffset);

        // Assert
        isWithinWindow.Should().BeTrue();
    }
}
