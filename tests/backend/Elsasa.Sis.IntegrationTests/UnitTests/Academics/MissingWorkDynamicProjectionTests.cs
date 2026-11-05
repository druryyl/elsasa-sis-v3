namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Data.Common;
using Elsasa.Sis.Academics.Features.Assignments.GetStudentCourseworkWorkspace;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for dynamic missing work status projection, countdown days, workspace filtering, and student authorization guards (TD-06, GAP-007, OQ-005, SCR-ACD-02, SCR-SELF-03, P5-S12).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class MissingWorkDynamicProjectionTests
{
    [Theory]
    [InlineData("submitted", true, "submitted")]
    [InlineData("submitted", false, "submitted")]
    [InlineData("late", true, "late")]
    [InlineData("late", false, "late")]
    [InlineData("excused", true, "excused")]
    [InlineData("excused", false, "excused")]
    [InlineData("exempted", true, "exempted")]
    [InlineData("exempted", false, "exempted")]
    public void DynamicStatus_Should_ReflectSubmissionStatus_When_SubmissionExists(
        string submissionStatus,
        bool isDueDatePassed,
        string expectedStatus)
    {
        // Act: Evaluate status projection simulating SQL CASE rule (TD-06)
        var actualStatus = EvaluateDisplayStatus(
            hasSubmission: true,
            submissionStatus: submissionStatus,
            hasDueDate: true,
            isDueDatePassed: isDueDatePassed);

        // Assert
        actualStatus.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(true, "missing")]
    [InlineData(false, "pending")]
    public void DynamicStatus_Should_EvaluateMissingOrPending_When_NoSubmissionExists(
        bool isDueDatePassed,
        string expectedStatus)
    {
        // Act: Evaluate status projection when no submission exists (TD-06)
        var actualStatus = EvaluateDisplayStatus(
            hasSubmission: false,
            submissionStatus: null,
            hasDueDate: true,
            isDueDatePassed: isDueDatePassed);

        // Assert
        actualStatus.Should().Be(expectedStatus);
    }

    [Fact]
    public void DynamicStatus_Should_BePending_When_NoDueDateSpecified()
    {
        // Act
        var actualStatus = EvaluateDisplayStatus(
            hasSubmission: false,
            submissionStatus: null,
            hasDueDate: false,
            isDueDatePassed: false);

        // Assert
        actualStatus.Should().Be("pending");
    }

    [Theory]
    [InlineData(0, 0)]   // Due today
    [InlineData(1, 1)]   // Due tomorrow
    [InlineData(5, 5)]   // Due in 5 days
    [InlineData(-1, -1)] // Overdue by 1 day
    [InlineData(-7, -7)] // Overdue by 7 days
    public void CountdownDays_Should_CalculateDifferenceFromToday(int offsetDays, int expectedCountdown)
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueDate = today.AddDays(offsetDays);

        // Act
        var countdown = dueDate.DayNumber - today.DayNumber;

        // Assert
        countdown.Should().Be(expectedCountdown);
    }

    [Theory]
    [InlineData("all", "pending", 3, true)]
    [InlineData("all", "missing", -2, true)]
    [InlineData("all", "submitted", 0, true)]
    [InlineData("upcoming", "pending", 5, true)]
    [InlineData("upcoming", "pending", 0, true)]
    [InlineData("upcoming", "missing", -1, false)]
    [InlineData("upcoming", "submitted", 2, false)]
    [InlineData("due_soon", "pending", 2, true)]
    [InlineData("due_soon", "pending", 0, true)]
    [InlineData("due_soon", "pending", 8, false)]
    [InlineData("due_soon", "missing", -1, false)]
    [InlineData("submitted", "submitted", 1, true)]
    [InlineData("submitted", "late", -1, true)]
    [InlineData("submitted", "excused", 0, true)]
    [InlineData("submitted", "exempted", 0, true)]
    [InlineData("submitted", "pending", 2, false)]
    [InlineData("submitted", "missing", -1, false)]
    [InlineData("missing", "missing", -1, true)]
    [InlineData("missing", "pending", 2, false)]
    [InlineData("missing", "submitted", -1, false)]
    public void WorkspaceFilter_Should_FilterTasksCorrectly(
        string filter,
        string displayStatus,
        int? countdownDays,
        bool shouldMatch)
    {
        // Act
        var matched = MatchesWorkspaceFilter(filter, displayStatus, countdownDays);

        // Assert
        matched.Should().Be(shouldMatch);
    }

    [Fact]
    public async Task GetStudentCourseworkWorkspaceHandler_Should_Fail_When_FilterIsInvalid()
    {
        // Arrange
        var connectionFactory = new DummyConnectionFactory();
        var currentUser = new FakeCurrentUser(
            id: Guid.NewGuid(),
            username: "student1",
            roles: ["Student"],
            studentId: Guid.NewGuid());

        var handler = new GetStudentCourseworkWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync("invalid_filter");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academic.InvalidStatusFilter");
    }

    [Fact]
    public async Task GetStudentCourseworkWorkspaceHandler_Should_Fail_When_UserIsNotStudentAndNotPrivileged()
    {
        // Arrange
        var connectionFactory = new DummyConnectionFactory();
        var currentUser = new FakeCurrentUser(
            id: Guid.NewGuid(),
            username: "guest1",
            roles: ["Guest"],
            studentId: null);

        var handler = new GetStudentCourseworkWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync("all");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academic.UnauthorizedStudentWorkspace");
    }

    [Fact]
    public async Task GetStudentCourseworkWorkspaceHandler_Should_Fail_When_StudentAttemptsToViewAnotherStudent()
    {
        // Arrange
        var connectionFactory = new DummyConnectionFactory();
        var ownStudentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser(
            id: Guid.NewGuid(),
            username: "student1",
            roles: ["Student"],
            studentId: ownStudentId);

        var handler = new GetStudentCourseworkWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync("all", otherStudentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academic.UnauthorizedStudentWorkspace");
    }

    [Fact]
    public async Task GetStudentCourseworkWorkspaceHandler_Should_Fail_When_PrivilegedUserOmitsStudentId()
    {
        // Arrange
        var connectionFactory = new DummyConnectionFactory();
        var currentUser = new FakeCurrentUser(
            id: Guid.NewGuid(),
            username: "teacher1",
            roles: ["Teacher"],
            studentId: null);

        var handler = new GetStudentCourseworkWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync("all", studentIdOverride: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academic.StudentIdRequired");
    }

    /// <summary>
    /// Simulates SQL CASE logic from TD-06:
    /// CASE
    ///     WHEN s.id IS NOT NULL THEN s.status
    ///     WHEN a.due_date IS NOT NULL AND a.due_date &lt; CAST(SYSUTCDATETIME() AS DATE) THEN 'missing'
    ///     ELSE 'pending'
    /// END
    /// </summary>
    private static string EvaluateDisplayStatus(
        bool hasSubmission,
        string? submissionStatus,
        bool hasDueDate,
        bool isDueDatePassed)
    {
        if (hasSubmission && !string.IsNullOrWhiteSpace(submissionStatus))
        {
            return submissionStatus;
        }

        if (hasDueDate && isDueDatePassed)
        {
            return "missing";
        }

        return "pending";
    }

    /// <summary>
    /// Simulates SQL WHERE filter criteria from GetStudentCourseworkWorkspaceHandler.
    /// </summary>
    private static bool MatchesWorkspaceFilter(string filter, string displayStatus, int? countdownDays)
    {
        var normalized = filter.ToLowerInvariant();
        return normalized switch
        {
            "all" => true,
            "submitted" => displayStatus is "submitted" or "late" or "excused" or "exempted",
            "missing" => displayStatus == "missing",
            "due_soon" => displayStatus == "pending" && countdownDays is >= 0 and <= 7,
            "upcoming" => displayStatus == "pending" && (countdownDays is null or >= 0),
            _ => displayStatus.Equals(normalized, StringComparison.OrdinalIgnoreCase)
        };
    }

    private sealed class FakeCurrentUser(
        Guid? id,
        string? username,
        IReadOnlyList<string> roles,
        Guid? studentId) : ICurrentUser
    {
        public Guid? Id => id;
        public string? Username => username;
        public string? Email => $"{username}@example.com";
        public IReadOnlyList<string> Roles => roles;
        public bool IsAuthenticated => id.HasValue;
        public Guid? StudentId => studentId;
    }

    private sealed class DummyConnectionFactory : ISqlConnectionFactory
    {
        public ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Not required for fast in-memory validator/guard tests.");
        }
    }
}
