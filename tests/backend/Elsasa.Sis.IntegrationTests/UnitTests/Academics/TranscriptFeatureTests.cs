namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Text.Json;
using Elsasa.Sis.Academics.Features.Transcripts;
using Elsasa.Sis.Academics.Features.Transcripts.GenerateInterimTranscript;
using Elsasa.Sis.Academics.Features.Transcripts.IssueTranscript;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for cumulative interim transcript generation, GPA calculation, snapshot freezing, and issuance events (UC-ACD-13, TD-05, P5-S13).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class TranscriptFeatureTests
{
    private readonly GenerateInterimTranscriptValidator _generateValidator = new();

    [Fact]
    public void GenerateValidator_Should_Succeed_When_ValidRequest()
    {
        // Arrange
        var request = new GenerateInterimTranscriptRequest(
            StudentId: Guid.NewGuid(),
            TranscriptType: "official",
            PeriodStart: new DateOnly(2025, 7, 1),
            PeriodEnd: new DateOnly(2026, 6, 30),
            Purpose: "University Admission",
            Notes: "Interim transcript requested by counselor.");

        // Act
        var result = _generateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void GenerateValidator_Should_Succeed_When_Unofficial_CaseInsensitive()
    {
        // Arrange
        var request = new GenerateInterimTranscriptRequest(
            StudentId: Guid.NewGuid(),
            TranscriptType: "UNOFFICIAL",
            PeriodStart: new DateOnly(2025, 7, 1),
            PeriodEnd: new DateOnly(2025, 12, 31));

        // Act
        var result = _generateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GenerateValidator_Should_Fail_When_StudentId_IsEmpty()
    {
        // Arrange
        var request = new GenerateInterimTranscriptRequest(
            StudentId: Guid.Empty,
            TranscriptType: "official",
            PeriodStart: new DateOnly(2025, 7, 1),
            PeriodEnd: new DateOnly(2026, 6, 30));

        // Act
        var result = _generateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateInterimTranscriptRequest.StudentId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("diploma")]
    [InlineData("certified")]
    public void GenerateValidator_Should_Fail_When_TranscriptType_IsInvalid(string invalidType)
    {
        // Arrange
        var request = new GenerateInterimTranscriptRequest(
            StudentId: Guid.NewGuid(),
            TranscriptType: invalidType,
            PeriodStart: new DateOnly(2025, 7, 1),
            PeriodEnd: new DateOnly(2026, 6, 30));

        // Act
        var result = _generateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateInterimTranscriptRequest.TranscriptType));
    }

    [Fact]
    public void GenerateValidator_Should_Fail_When_PeriodEnd_Precedes_PeriodStart()
    {
        // Arrange
        var request = new GenerateInterimTranscriptRequest(
            StudentId: Guid.NewGuid(),
            TranscriptType: "official",
            PeriodStart: new DateOnly(2026, 7, 1),
            PeriodEnd: new DateOnly(2025, 6, 30));

        // Act
        var result = _generateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateInterimTranscriptRequest.PeriodEnd));
    }

    [Fact]
    public void GenerateValidator_Should_Fail_When_Purpose_Exceeds_200Chars()
    {
        // Arrange
        var request = new GenerateInterimTranscriptRequest(
            StudentId: Guid.NewGuid(),
            TranscriptType: "official",
            PeriodStart: new DateOnly(2025, 7, 1),
            PeriodEnd: new DateOnly(2026, 6, 30),
            Purpose: new string('P', 201));

        // Act
        var result = _generateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateInterimTranscriptRequest.Purpose));
    }

    [Fact]
    public void TranscriptGpaCalculator_Should_Return_Null_When_Courses_IsEmptyOrNull()
    {
        TranscriptGpaCalculator.CalculatePeriodGpa(null).Should().BeNull();
        TranscriptGpaCalculator.CalculatePeriodGpa([]).Should().BeNull();
    }

    [Fact]
    public void TranscriptGpaCalculator_Should_Calculate_Weighted_Gpa_Correctly()
    {
        // Arrange:
        // Course 1: Math, Credits = 3.0, Grade = "A" (4.0) -> points = 12.0
        // Course 2: English, Credits = 2.0, Grade = "B" (3.0) -> points = 6.0
        // Total credits = 5.0, Total points = 18.0 -> GPA = 3.60
        var courses = new List<TranscriptCourseEntryDto>
        {
            new(Guid.NewGuid(), "MATH101", "Mathematics", 3.0m, 92m, "A", false),
            new(Guid.NewGuid(), "ENG101", "English", 2.0m, 82m, "B", false)
        };

        // Act
        var gpa = TranscriptGpaCalculator.CalculatePeriodGpa(courses);

        // Assert
        gpa.Should().Be(3.60m);
    }

    [Fact]
    public void TranscriptGpaCalculator_Should_Handle_Transferred_Credits_Correctly()
    {
        // Arrange:
        // Course 1: Transfer Biology [T], Credits = 4.0, Grade = "A" (4.0) -> points = 16.0
        // Course 2: Physics, Credits = 4.0, Grade = "C" (2.0) -> points = 8.0
        // Total credits = 8.0, Total points = 24.0 -> GPA = 3.00
        var courses = new List<TranscriptCourseEntryDto>
        {
            new(Guid.NewGuid(), "BIO100", "General Biology", 4.0m, null, "A", true),
            new(Guid.NewGuid(), "PHYS100", "Physics", 4.0m, 68m, "C", false)
        };

        // Act
        var gpa = TranscriptGpaCalculator.CalculatePeriodGpa(courses);

        // Assert
        gpa.Should().Be(3.00m);
    }

    [Fact]
    public void TranscriptGpaCalculator_Should_Fallback_To_ScoreNumeric_When_GradeLetter_IsAbsent()
    {
        // Course with 88 score -> grade point 4.0
        // Course with 55 score -> grade point 1.0
        var courses = new List<TranscriptCourseEntryDto>
        {
            new(Guid.NewGuid(), "ART101", "Art History", 2.0m, 88m, null, false),
            new(Guid.NewGuid(), "MUS101", "Music", 2.0m, 55m, null, false)
        };

        // (2*4 + 2*1) / 4 = 10 / 4 = 2.50
        var gpa = TranscriptGpaCalculator.CalculatePeriodGpa(courses);

        gpa.Should().Be(2.50m);
    }

    [Fact]
    public void TranscriptSnapshotDto_Should_Serialize_And_Deserialize_Faithfully()
    {
        // Arrange
        var transcriptId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var courses = new List<TranscriptCourseEntryDto>
        {
            new(Guid.NewGuid(), "CS101", "Computer Science", 3.0m, 95m, "A", false),
            new(Guid.NewGuid(), "HIST101", "World History", 3.0m, null, "A", true) // [T] transfer credit
        };
        var completedPeriods = new List<TranscriptPeriodSummaryDto>
        {
            new(periodId, "2025/2026 Semester 1", courses, 4.0m)
        };

        var snapshot = new TranscriptSnapshotDto(
            TranscriptId: transcriptId,
            StudentId: studentId,
            StudentName: "Budi Santoso",
            StudentNisn: "0012345678",
            TranscriptType: "official",
            PeriodStart: new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Status: "issued",
            Purpose: "Scholarship Application",
            CompletedPeriods: completedPeriods);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var json = JsonSerializer.Serialize(snapshot, options);
        var deserialized = JsonSerializer.Deserialize<TranscriptSnapshotDto>(json, options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TranscriptId.Should().Be(transcriptId);
        deserialized.StudentId.Should().Be(studentId);
        deserialized.StudentName.Should().Be("Budi Santoso");
        deserialized.StudentNisn.Should().Be("0012345678");
        deserialized.TranscriptType.Should().Be("official");
        deserialized.Status.Should().Be("issued");
        deserialized.CompletedPeriods.Should().HaveCount(1);
        deserialized.CompletedPeriods[0].Courses.Should().HaveCount(2);
        deserialized.CompletedPeriods[0].Courses[1].IsTransferred.Should().BeTrue();
        deserialized.CompletedPeriods[0].Gpa.Should().Be(4.0m);
    }

    [Fact]
    public void TranscriptIssuedEvent_Should_Initialize_Properties_Correctly()
    {
        // Arrange
        var transcriptId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        // Act
        var domainEvent = new TranscriptIssuedEvent(transcriptId, studentId, "official", "Job Application");

        // Assert
        domainEvent.TranscriptId.Should().Be(transcriptId);
        domainEvent.StudentId.Should().Be(studentId);
        domainEvent.TranscriptType.Should().Be("official");
        domainEvent.Purpose.Should().Be("Job Application");
        domainEvent.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
