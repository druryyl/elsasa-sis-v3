namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Text.Json;
using Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for session attendance roll-call validator and request serialization (P4-S08, TD-04, UC-ACD-03).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class AttendanceValidatorTests
{
    private readonly RecordSessionAttendanceValidator _validator = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void AttendanceValidator_Should_Succeed_When_EntriesAreValid()
    {
        // Arrange
        var request = new RecordSessionAttendanceRequest(
        [
            new StudentAttendanceEntry(Guid.NewGuid(), "present", null, "On time"),
            new StudentAttendanceEntry(Guid.NewGuid(), "absent", null, "Unexcused"),
            new StudentAttendanceEntry(Guid.NewGuid(), "late", null, "10 minutes late"),
            new StudentAttendanceEntry(Guid.NewGuid(), "excused_absence", "http://docs/med-cert.pdf", "Doctor note"),
            new StudentAttendanceEntry(Guid.NewGuid(), "partial", null, "Left early for appointment")
        ]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("present")]
    [InlineData("absent")]
    [InlineData("late")]
    [InlineData("excused_absence")]
    [InlineData("partial")]
    [InlineData("PRESENT")]
    [InlineData("Late")]
    public void AttendanceValidator_Should_Succeed_For_AllValidStatusesCaseInsensitively(string status)
    {
        // Arrange
        var request = new RecordSessionAttendanceRequest(
        [
            new StudentAttendanceEntry(Guid.NewGuid(), status)
        ]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AttendanceValidator_Should_Fail_When_EntriesAreNull()
    {
        // Arrange
        var request = new RecordSessionAttendanceRequest(null!);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Entries");
    }

    [Fact]
    public void AttendanceValidator_Should_Fail_When_EntriesAreEmpty()
    {
        // Arrange
        var request = new RecordSessionAttendanceRequest([]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("At least one attendance entry must be provided"));
    }

    [Fact]
    public void AttendanceValidator_Should_Fail_When_DuplicateStudentsExist()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var request = new RecordSessionAttendanceRequest(
        [
            new StudentAttendanceEntry(studentId, "present"),
            new StudentAttendanceEntry(studentId, "late")
        ]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("duplicate students"));
    }

    [Fact]
    public void AttendanceValidator_Should_Fail_When_StudentIdIsEmpty()
    {
        // Arrange
        var request = new RecordSessionAttendanceRequest(
        [
            new StudentAttendanceEntry(Guid.Empty, "present")
        ]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Student ID is required"));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("sick")]
    [InlineData("truant")]
    [InlineData("")]
    public void AttendanceValidator_Should_Fail_When_StatusIsInvalid(string invalidStatus)
    {
        // Arrange
        var request = new RecordSessionAttendanceRequest(
        [
            new StudentAttendanceEntry(Guid.NewGuid(), invalidStatus)
        ]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Attendance status must be one of"));
    }

    [Fact]
    public void RequestJsonConverter_Should_Deserialize_WrappedObject()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var json = $$"""
            {
                "entries": [
                    {
                        "studentId": "{{studentId}}",
                        "status": "present",
                        "notes": "Good participation"
                    }
                ]
            }
            """;

        // Act
        var request = JsonSerializer.Deserialize<RecordSessionAttendanceRequest>(json, _jsonOptions);

        // Assert
        request.Should().NotBeNull();
        request!.Entries.Should().HaveCount(1);
        request.Entries[0].StudentId.Should().Be(studentId);
        request.Entries[0].Status.Should().Be("present");
        request.Entries[0].Notes.Should().Be("Good participation");
    }

    [Fact]
    public void RequestJsonConverter_Should_Deserialize_RootArray()
    {
        // Arrange
        var studentId1 = Guid.NewGuid();
        var studentId2 = Guid.NewGuid();
        var json = $$"""
            [
                {
                    "studentId": "{{studentId1}}",
                    "status": "present"
                },
                {
                    "studentId": "{{studentId2}}",
                    "status": "absent",
                    "evidence": "http://notes/123"
                }
            ]
            """;

        // Act
        var request = JsonSerializer.Deserialize<RecordSessionAttendanceRequest>(json, _jsonOptions);

        // Assert
        request.Should().NotBeNull();
        request!.Entries.Should().HaveCount(2);
        request.Entries[0].StudentId.Should().Be(studentId1);
        request.Entries[0].Status.Should().Be("present");
        request.Entries[1].StudentId.Should().Be(studentId2);
        request.Entries[1].Status.Should().Be("absent");
        request.Entries[1].Evidence.Should().Be("http://notes/123");
    }

    [Theory]
    [InlineData("present")]
    [InlineData("absent")]
    [InlineData("late")]
    [InlineData("excused_absence")]
    [InlineData("partial")]
    [InlineData("PRESENT")]
    [InlineData("Late")]
    public void CorrectAttendanceValidator_Should_Succeed_When_RequestIsValid(string status)
    {
        // Arrange
        var validator = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceValidator();
        var request = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceRequest(
            status,
            "Valid reason for correction",
            "http://evidence/doc.pdf");

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CorrectAttendanceValidator_Should_Fail_When_StatusIsEmpty()
    {
        // Arrange
        var validator = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceValidator();
        var request = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceRequest(
            "",
            "Valid correction reason");

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewStatus");
    }

    [Fact]
    public void CorrectAttendanceValidator_Should_Fail_When_StatusIsInvalid()
    {
        // Arrange
        var validator = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceValidator();
        var request = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceRequest(
            "unknown_status",
            "Valid correction reason");

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewStatus" && e.ErrorMessage.Contains("Attendance status must be one of"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CorrectAttendanceValidator_Should_Fail_When_ReasonIsEmpty(string? reason)
    {
        // Arrange
        var validator = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceValidator();
        var request = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceRequest(
            "present",
            reason!);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void CorrectAttendanceValidator_Should_Fail_When_ReasonIsLessThan5Chars()
    {
        // Arrange
        var validator = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceValidator();
        var request = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceRequest(
            "present",
            "Fix");

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage.Contains("at least 5 characters"));
    }

    [Fact]
    public void CorrectAttendanceValidator_Should_Fail_When_ReasonExceeds2000Chars()
    {
        // Arrange
        var validator = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceValidator();
        var request = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceRequest(
            "present",
            new string('A', 2001));

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage.Contains("not exceed 2000 characters"));
    }

    [Fact]
    public void CorrectAttendanceValidator_Should_Fail_When_EvidenceExceeds2000Chars()
    {
        // Arrange
        var validator = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceValidator();
        var request = new Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance.CorrectAttendanceRequest(
            "present",
            "Valid justification reason",
            new string('B', 2001));

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Evidence" && e.ErrorMessage.Contains("not exceed 2000 characters"));
    }
}
