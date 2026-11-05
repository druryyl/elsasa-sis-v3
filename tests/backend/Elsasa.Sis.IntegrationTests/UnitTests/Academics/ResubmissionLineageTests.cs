namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for coursework submission lineage tracking and timeliness evaluation rules (P6-S14, TD-05, TD-06, UC-ACD-01).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class ResubmissionLineageTests
{
    private static readonly DateOnly _today = new(2026, 11, 10);

    #region Resubmission Lineage Tests

    [Fact]
    public void DetermineLineage_Should_IdentifyInitialSubmission_When_NoPriorSubmissionExists()
    {
        // Arrange
        Guid? priorSubmissionId = null;

        // Act
        var (isResubmission, previousSubmissionId) = CourseworkSubmissionEvaluator.DetermineLineage(priorSubmissionId);

        // Assert
        isResubmission.Should().BeFalse("first-time submissions are not resubmissions");
        previousSubmissionId.Should().BeNull("initial submissions do not have a predecessor");
    }

    [Fact]
    public void DetermineLineage_Should_IdentifyResubmission_When_PriorSubmissionExists()
    {
        // Arrange
        var initialSubmissionId = Guid.NewGuid();

        // Act
        var (isResubmission, previousSubmissionId) = CourseworkSubmissionEvaluator.DetermineLineage(initialSubmissionId);

        // Assert
        isResubmission.Should().BeTrue("subsequent submissions must be marked as resubmissions");
        previousSubmissionId.Should().Be(initialSubmissionId, "resubmissions must link to their immediate predecessor");
    }

    [Fact]
    public void DetermineLineage_Should_MaintainChainedLineage_AcrossMultipleSubmissions()
    {
        // Arrange
        var submission1Id = Guid.NewGuid();
        var submission2Id = Guid.NewGuid();

        // Act - submission 1 (first submission)
        var lineage1 = CourseworkSubmissionEvaluator.DetermineLineage(null);

        // Act - submission 2 (first resubmission)
        var lineage2 = CourseworkSubmissionEvaluator.DetermineLineage(submission1Id);

        // Act - submission 3 (second resubmission)
        var lineage3 = CourseworkSubmissionEvaluator.DetermineLineage(submission2Id);

        // Assert
        lineage1.IsResubmission.Should().BeFalse();
        lineage1.PreviousSubmissionId.Should().BeNull();

        lineage2.IsResubmission.Should().BeTrue();
        lineage2.PreviousSubmissionId.Should().Be(submission1Id);

        lineage3.IsResubmission.Should().BeTrue();
        lineage3.PreviousSubmissionId.Should().Be(submission2Id);
    }

    #endregion

    #region Timeliness Evaluation Tests

    [Fact]
    public void EvaluateTimeliness_Should_BeSubmitted_When_NoDueDateIsSpecified()
    {
        // Arrange
        DateOnly? dueDate = null;

        // Act
        var status = CourseworkSubmissionEvaluator.EvaluateTimeliness(dueDate, _today);

        // Assert
        status.Should().Be("submitted", "assignments without due dates are never late");
    }

    [Fact]
    public void EvaluateTimeliness_Should_BeSubmitted_When_SubmittedBeforeDueDate()
    {
        // Arrange
        var dueDate = _today.AddDays(3);

        // Act
        var status = CourseworkSubmissionEvaluator.EvaluateTimeliness(dueDate, _today);

        // Assert
        status.Should().Be("submitted", "submissions prior to due date are on-time");
    }

    [Fact]
    public void EvaluateTimeliness_Should_BeSubmitted_When_SubmittedOnDueDate()
    {
        // Arrange
        var dueDate = _today;

        // Act
        var status = CourseworkSubmissionEvaluator.EvaluateTimeliness(dueDate, _today);

        // Assert
        status.Should().Be("submitted", "submissions on the due date are on-time");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    public void EvaluateTimeliness_Should_BeLate_When_SubmittedAfterDueDate(int daysLate)
    {
        // Arrange
        var dueDate = _today.AddDays(-daysLate);

        // Act
        var status = CourseworkSubmissionEvaluator.EvaluateTimeliness(dueDate, _today);

        // Assert
        status.Should().Be("late", $"submissions {daysLate} days after due date must be marked as late");
    }

    #endregion
}
