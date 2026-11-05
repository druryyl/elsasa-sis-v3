namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Text.Json;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.ApproveReportCard;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for report card certification, snapshot freezing, publication, and query slices (GAP-006, TD-05, UC-ACD-13, P5-S12).
/// Verifies validator constraints, snapshot serialization round-tripping, domain error contracts, and domain events.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class ReportCardCertificationTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly ApproveReportCardValidator _approveValidator = new();

    [Fact]
    public void ApproveValidator_Should_Pass_When_NarrativeComments_Is_Null_Or_Empty()
    {
        // Arrange
        var request1 = new ApproveReportCardRequest(null);
        var request2 = new ApproveReportCardRequest(string.Empty);

        // Act & Assert
        _approveValidator.Validate(request1).IsValid.Should().BeTrue();
        _approveValidator.Validate(request2).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ApproveValidator_Should_Pass_When_NarrativeComments_Is_Within_Limit()
    {
        // Arrange
        var request = new ApproveReportCardRequest("Student demonstrated excellent critical thinking and consistency.");

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ApproveValidator_Should_Fail_When_NarrativeComments_Exceeds_Max_Length()
    {
        // Arrange
        var longComments = new string('A', 4001);
        var request = new ApproveReportCardRequest(longComments);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.NarrativeComments));
    }

    [Fact]
    public void ReportCardSnapshot_Should_RoundTrip_Through_Json_Serialization_Without_Loss()
    {
        // Arrange
        var reportCardId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var learningUnitId1 = Guid.NewGuid();
        var learningUnitId2 = Guid.NewGuid();

        var subjectEntries = new List<ReportCardSubjectEntryDto>
        {
            new(
                LearningUnitId: learningUnitId1,
                SubjectCode: "MAT-401",
                SubjectTitle: "Matematika Tingkat Lanjut",
                ScoreNumeric: 88.5m,
                GradeLetter: "A",
                GradeLabel: "Sangat Mahir",
                TeacherComment: "Menunjukkan pemahaman konsep aljabar yang mendalam.",
                DisplayOrder: 1),
            new(
                LearningUnitId: learningUnitId2,
                SubjectCode: "ENG-401",
                SubjectTitle: "English Academic Literacy",
                ScoreNumeric: 92.0m,
                GradeLetter: "A+",
                GradeLabel: "Cakap",
                TeacherComment: "Excellent oral presentation and active seminar participation.",
                DisplayOrder: 2)
        };

        var attendance = new ReportCardAttendanceSummaryDto(
            PresentDays: 85,
            AbsentDays: 1,
            LateDays: 2,
            ExcusedDays: 2);

        var originalSnapshot = new ReportCardSnapshotDto(
            ReportCardId: reportCardId,
            StudentId: studentId,
            StudentName: "Ahmad Fauzi",
            StudentNisn: "0012345678",
            AcademicPeriodId: periodId,
            PeriodLabel: "Semester 1 2026/2027",
            ReportType: "end_of_term",
            TemplateName: "Buku Rapor Merdeka",
            Status: "approved",
            ApprovedBy: "drury.authority",
            ApprovedDate: DateTime.UtcNow,
            NarrativeComments: "Siswa menunjukkan perkembangan karakter Profil Pelajar Pancasila yang luar biasa.",
            SubjectEntries: subjectEntries,
            AttendanceSummary: attendance);

        // Act
        var json = JsonSerializer.Serialize(originalSnapshot, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ReportCardSnapshotDto>(json, _jsonOptions);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        deserialized.Should().NotBeNull();
        deserialized!.ReportCardId.Should().Be(reportCardId);
        deserialized.StudentId.Should().Be(studentId);
        deserialized.StudentName.Should().Be("Ahmad Fauzi");
        deserialized.StudentNisn.Should().Be("0012345678");
        deserialized.PeriodLabel.Should().Be("Semester 1 2026/2027");
        deserialized.ReportType.Should().Be("end_of_term");
        deserialized.TemplateName.Should().Be("Buku Rapor Merdeka");
        deserialized.Status.Should().Be("approved");
        deserialized.ApprovedBy.Should().Be("drury.authority");
        deserialized.NarrativeComments.Should().Be(originalSnapshot.NarrativeComments);

        deserialized.SubjectEntries.Should().HaveCount(2);
        deserialized.SubjectEntries[0].SubjectCode.Should().Be("MAT-401");
        deserialized.SubjectEntries[0].ScoreNumeric.Should().Be(88.5m);
        deserialized.SubjectEntries[0].GradeLetter.Should().Be("A");
        deserialized.SubjectEntries[1].SubjectCode.Should().Be("ENG-401");
        deserialized.SubjectEntries[1].ScoreNumeric.Should().Be(92.0m);

        deserialized.AttendanceSummary.Should().NotBeNull();
        deserialized.AttendanceSummary!.PresentDays.Should().Be(85);
        deserialized.AttendanceSummary.AbsentDays.Should().Be(1);
    }

    [Fact]
    public void DomainErrors_ReportCards_Should_Define_All_Required_Lifecycle_Errors()
    {
        // Assert
        DomainErrors.Academics.ReportCards.NotFound.Code.Should().Be("Academics.ReportCards.NotFound");
        DomainErrors.Academics.ReportCards.AlreadyExists.Code.Should().Be("Academics.ReportCards.AlreadyExists");
        DomainErrors.Academics.ReportCards.NoActiveParticipations.Code.Should().Be("Academics.ReportCards.NoActiveParticipations");
        DomainErrors.Academics.ReportCards.NotDraft.Code.Should().Be("Academics.ReportCards.NotDraft");
        DomainErrors.Academics.ReportCards.NotApproved.Code.Should().Be("Academics.ReportCards.NotApproved");
        DomainErrors.Academics.ReportCards.AlreadyApproved.Code.Should().Be("Academics.ReportCards.AlreadyApproved");
        DomainErrors.Academics.ReportCards.AlreadyPublished.Code.Should().Be("Academics.ReportCards.AlreadyPublished");
    }

    [Fact]
    public void ReportCardPublishedEvent_Should_Initialize_Correctly()
    {
        // Arrange
        var reportCardId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        const string reportType = "end_of_term";

        // Act
        var domainEvent = new ReportCardPublishedEvent(
            reportCardId,
            studentId,
            periodId,
            reportType);

        // Assert
        domainEvent.ReportCardId.Should().Be(reportCardId);
        domainEvent.StudentId.Should().Be(studentId);
        domainEvent.AcademicPeriodId.Should().Be(periodId);
        domainEvent.ReportType.Should().Be(reportType);
        domainEvent.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ReportCardDetailDto_Should_Support_Both_Certified_And_Live_Draft_Modes()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        // Draft mode
        var draftDetail = new ReportCardDetailDto(
            Id: id,
            StudentId: studentId,
            StudentName: "Budi Santoso",
            StudentNisn: "0099887766",
            AcademicPeriodId: periodId,
            PeriodLabel: "Term 1",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            Status: "draft",
            ApprovedBy: null,
            ApprovedDate: null,
            PublishedDate: null,
            NarrativeComments: null,
            ContentSnapshot: null,
            Snapshot: null,
            Entries: new List<ReportCardEntryDto>(),
            AttendanceSummary: new ReportCardAttendanceSummaryDto(0, 0, 0, 0),
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedBy: "coordinator1");

        // Assert draft mode
        draftDetail.Status.Should().Be("draft");
        draftDetail.Snapshot.Should().BeNull();
        draftDetail.ContentSnapshot.Should().BeNull();

        // Certified mode
        var certifiedDetail = new ReportCardDetailDto(
            Id: id,
            StudentId: studentId,
            StudentName: "Budi Santoso",
            StudentNisn: "0099887766",
            AcademicPeriodId: periodId,
            PeriodLabel: "Term 1",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            Status: "approved",
            ApprovedBy: "authority1",
            ApprovedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            PublishedDate: null,
            NarrativeComments: "Progressing well",
            ContentSnapshot: "{\"Status\":\"approved\"}",
            Snapshot: null,
            Entries: new List<ReportCardEntryDto>(),
            AttendanceSummary: new ReportCardAttendanceSummaryDto(90, 0, 1, 1),
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedBy: "coordinator1");

        // Assert certified mode
        certifiedDetail.Status.Should().Be("approved");
        certifiedDetail.ApprovedBy.Should().Be("authority1");
        certifiedDetail.ContentSnapshot.Should().NotBeNull();
    }
}
