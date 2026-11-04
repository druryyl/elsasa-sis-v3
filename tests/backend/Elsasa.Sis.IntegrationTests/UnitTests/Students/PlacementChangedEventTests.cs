namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Contracts.Common;
using Elsasa.Sis.Contracts.Students.Events;
using FluentAssertions;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class PlacementChangedEventTests
{
    [Fact]
    public void PrimaryConstructor_Should_SetAllPropertiesCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var placementId = Guid.NewGuid();
        var campus = "Main Campus";
        var schoolLevel = "High School";
        var grade = "Grade 11";
        var academicYear = "2026/2027";
        var effectiveDate = new DateOnly(2026, 9, 1);
        var program = "IB Diploma";
        var stream = "Science";
        var cohort = "2026-Science-A";
        var homeroom = "Room 301";
        var occurredOn = DateTimeOffset.UtcNow.AddMinutes(-5);

        // Act
        var @event = new PlacementChangedEvent(
            StudentId: studentId,
            PlacementId: placementId,
            Campus: campus,
            SchoolLevel: schoolLevel,
            Grade: grade,
            AcademicYear: academicYear,
            EffectiveDate: effectiveDate,
            Program: program,
            Stream: stream,
            Cohort: cohort,
            Homeroom: homeroom,
            OccurredOn: occurredOn);

        // Assert
        @event.StudentId.Should().Be(studentId);
        @event.PlacementId.Should().Be(placementId);
        @event.Campus.Should().Be(campus);
        @event.SchoolLevel.Should().Be(schoolLevel);
        @event.Grade.Should().Be(grade);
        @event.AcademicYear.Should().Be(academicYear);
        @event.EffectiveDate.Should().Be(effectiveDate);
        @event.Program.Should().Be(program);
        @event.Stream.Should().Be(stream);
        @event.Cohort.Should().Be(cohort);
        @event.Homeroom.Should().Be(homeroom);
        @event.OccurredOn.Should().Be(occurredOn);
        @event.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void ConvenienceConstructor_Should_DefaultOccurredOnToUtcNow()
    {
        // Arrange
        var beforeTime = DateTimeOffset.UtcNow;
        var studentId = Guid.NewGuid();
        var placementId = Guid.NewGuid();
        var campus = "North Campus";
        var schoolLevel = "Primary";
        var grade = "Grade 2";
        var academicYear = "2026/2027";
        var effectiveDate = new DateOnly(2026, 9, 1);

        // Act
        var @event = new PlacementChangedEvent(
            studentId,
            placementId,
            campus,
            schoolLevel,
            grade,
            academicYear,
            effectiveDate);
        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        @event.StudentId.Should().Be(studentId);
        @event.PlacementId.Should().Be(placementId);
        @event.Campus.Should().Be(campus);
        @event.SchoolLevel.Should().Be(schoolLevel);
        @event.Grade.Should().Be(grade);
        @event.AcademicYear.Should().Be(academicYear);
        @event.EffectiveDate.Should().Be(effectiveDate);
        @event.Program.Should().BeNull();
        @event.Stream.Should().BeNull();
        @event.Cohort.Should().BeNull();
        @event.Homeroom.Should().BeNull();
        @event.OccurredOn.Should().BeOnOrAfter(beforeTime).And.BeOnOrBefore(afterTime);
    }

    [Fact]
    public void ConvenienceConstructor_Should_AcceptOptionalParameters()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var placementId = Guid.NewGuid();
        var campus = "South Campus";
        var schoolLevel = "Middle School";
        var grade = "Grade 7";
        var academicYear = "2026/2027";
        var effectiveDate = new DateOnly(2026, 9, 1);
        var program = "Standard";
        var stream = "General";
        var cohort = "2026-MS";
        var homeroom = "Room 205";

        // Act
        var @event = new PlacementChangedEvent(
            studentId,
            placementId,
            campus,
            schoolLevel,
            grade,
            academicYear,
            effectiveDate,
            program,
            stream,
            cohort,
            homeroom);

        // Assert
        @event.Program.Should().Be(program);
        @event.Stream.Should().Be(stream);
        @event.Cohort.Should().Be(cohort);
        @event.Homeroom.Should().Be(homeroom);
    }
}
