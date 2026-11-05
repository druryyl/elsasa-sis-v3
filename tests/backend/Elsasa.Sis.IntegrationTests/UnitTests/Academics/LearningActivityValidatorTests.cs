namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.LearningActivities.CancelLearningActivity;
using Elsasa.Sis.Academics.Features.LearningActivities.ConductLearningActivity;
using Elsasa.Sis.Academics.Features.LearningActivities.PlanLearningActivity;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Learning Activity validators (P3-S07, TD-03).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class LearningActivityValidatorTests
{
    private readonly PlanLearningActivityValidator _planValidator = new();
    private readonly ConductLearningActivityValidator _conductValidator = new();
    private readonly CancelLearningActivityValidator _cancelValidator = new();

    #region PlanLearningActivityValidator Tests

    [Fact]
    public void PlanValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.NewGuid(),
            ActivityType: "lesson",
            Title: "Introduction to Fractions",
            Description: "Covers numerator and denominator basics.",
            SectionLabel: "4A",
            ActivityDate: new DateOnly(2026, 11, 10),
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(9, 30),
            TeacherAssignmentId: Guid.NewGuid(),
            TeacherName: "Budi Santoso",
            IsAttendanceRequired: true);

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("lesson")]
    [InlineData("tutorial")]
    [InlineData("laboratory")]
    [InlineData("independent_work")]
    [InlineData("project")]
    [InlineData("discussion")]
    [InlineData("practice")]
    [InlineData("examination")]
    [InlineData("remediation")]
    [InlineData("homeroom_session")]
    [InlineData("other")]
    [InlineData("LESSON")]
    [InlineData("Homeroom_Session")]
    public void PlanValidator_Should_Succeed_For_AllAllowedActivityTypes(string activityType)
    {
        // Arrange
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.NewGuid(),
            ActivityType: activityType,
            Title: "Session Title",
            SectionLabel: "4A",
            ActivityDate: new DateOnly(2026, 11, 10));

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]   // Current date (today)
    [InlineData(1)]   // Tomorrow
    [InlineData(30)]  // Future month
    public void PlanValidator_Should_Succeed_For_CurrentAndFutureDates(int dayOffset)
    {
        // Arrange
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(dayOffset);
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.NewGuid(),
            ActivityType: "lesson",
            Title: "Scheduled Class Session",
            ActivityDate: targetDate);

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PlanValidator_Should_Fail_When_LearningUnitIdIsEmpty()
    {
        // Arrange
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.Empty,
            ActivityType: "lesson",
            Title: "Math Lesson",
            ActivityDate: new DateOnly(2026, 11, 10));

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PlanLearningActivityRequest.LearningUnitId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid_type")]
    [InlineData("recess")]
    public void PlanValidator_Should_Fail_When_ActivityTypeIsInvalid(string invalidType)
    {
        // Arrange
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.NewGuid(),
            ActivityType: invalidType,
            Title: "Invalid Lesson",
            ActivityDate: new DateOnly(2026, 11, 10));

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PlanLearningActivityRequest.ActivityType));
    }

    [Fact]
    public void PlanValidator_Should_Fail_When_TitleIsEmpty()
    {
        // Arrange
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.NewGuid(),
            ActivityType: "lesson",
            Title: "",
            ActivityDate: new DateOnly(2026, 11, 10));

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PlanLearningActivityRequest.Title));
    }

    [Fact]
    public void PlanValidator_Should_Fail_When_TitleExceeds200Characters()
    {
        // Arrange
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.NewGuid(),
            ActivityType: "lesson",
            Title: new string('A', 201),
            ActivityDate: new DateOnly(2026, 11, 10));

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PlanLearningActivityRequest.Title));
    }

    [Fact]
    public void PlanValidator_Should_Fail_When_ActivityDateIsDefault()
    {
        // Arrange
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.NewGuid(),
            ActivityType: "lesson",
            Title: "Valid Title",
            ActivityDate: default);

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PlanLearningActivityRequest.ActivityDate));
    }

    [Fact]
    public void PlanValidator_Should_Fail_When_StartTimeIsLaterThanOrEqualToEndTime()
    {
        // Arrange
        var request = new PlanLearningActivityRequest(
            LearningUnitId: Guid.NewGuid(),
            ActivityType: "lesson",
            Title: "Math Lesson",
            ActivityDate: new DateOnly(2026, 11, 10),
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(9, 0));

        // Act
        var result = _planValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartTime");
    }

    #endregion

    #region ConductLearningActivityValidator Tests

    [Fact]
    public void ConductValidator_Should_Succeed_When_PayloadIsValid()
    {
        // Arrange
        var request = new ConductLearningActivityRequest(
            DeliveryNotes: "Delivered chapter 3 milestones.",
            ActualTeacherName: "Ibu Siti");

        // Act
        var result = _conductValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConductValidator_Should_Fail_When_ActualTeacherNameExceeds200Characters()
    {
        // Arrange
        var request = new ConductLearningActivityRequest(
            DeliveryNotes: "Notes",
            ActualTeacherName: new string('T', 201));

        // Act
        var result = _conductValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConductLearningActivityRequest.ActualTeacherName));
    }

    #endregion

    #region CancelLearningActivityValidator Tests

    [Fact]
    public void CancelValidator_Should_Succeed_When_ReasonIsValid()
    {
        // Arrange
        var request = new CancelLearningActivityRequest("School-wide assembly rescheduled this session.");

        // Act
        var result = _cancelValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("No")]
    public void CancelValidator_Should_Fail_When_ReasonIsTooShortOrEmpty(string reason)
    {
        // Arrange
        var request = new CancelLearningActivityRequest(reason);

        // Act
        var result = _cancelValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CancelLearningActivityRequest.CancellationReason));
    }

    [Fact]
    public void CancelValidator_Should_Fail_When_ReasonExceeds1000Characters()
    {
        // Arrange
        var request = new CancelLearningActivityRequest(new string('C', 1001));

        // Act
        var result = _cancelValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CancelLearningActivityRequest.CancellationReason));
    }

    #endregion
}
