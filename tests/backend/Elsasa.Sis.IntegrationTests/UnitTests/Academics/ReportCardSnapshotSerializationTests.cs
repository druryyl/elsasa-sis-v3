namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Text.Json;
using Elsasa.Sis.Contracts.Academics;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests verifying round-trip JSON serialization and deserialization integrity for
/// <see cref="ReportCardSnapshotDto"/> and <see cref="TranscriptSnapshotDto"/> (GAP-006, UC-ACD-13, TD-05, P7-S16).
/// </summary>
[Trait("Category", "UnitTest")]
[Trait("Category", "Unit")]
public sealed class ReportCardSnapshotSerializationTests
{
    private static readonly JsonSerializerOptions _webOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    #region ReportCardSnapshotDto Serialization Tests

    [Fact]
    public void ReportCardSnapshot_Should_RoundTrip_Successfully_With_Full_Payload()
    {
        // Arrange
        var reportCardId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();
        var approvedDate = new DateTime(2026, 12, 15, 10, 30, 0, DateTimeKind.Utc);

        var subject1 = new ReportCardSubjectEntryDto(
            LearningUnitId: Guid.NewGuid(),
            SubjectCode: "MAT-04",
            SubjectTitle: "Matematika Tingkat Dasar",
            ScoreNumeric: 87.50m,
            GradeLetter: "A",
            GradeLabel: "Sangat Mahir",
            TeacherComment: "Menunjukkan pemahaman sangat mendalam pada konsep geometri dan pecahan desimal.",
            DisplayOrder: 1);

        var subject2 = new ReportCardSubjectEntryDto(
            LearningUnitId: Guid.NewGuid(),
            SubjectCode: "IND-04",
            SubjectTitle: "Bahasa Indonesia",
            ScoreNumeric: 91.25m,
            GradeLetter: "A+",
            GradeLabel: "Istimewa",
            TeacherComment: "Mampu menyusun teks laporan observasi dengan struktur kalimat efektif.",
            DisplayOrder: 2);

        var attendance = new ReportCardAttendanceSummaryDto(
            PresentDays: 95,
            AbsentDays: 1,
            LateDays: 2,
            ExcusedDays: 2);

        var originalSnapshot = new ReportCardSnapshotDto(
            ReportCardId: reportCardId,
            StudentId: studentId,
            StudentName: "Ahmad Fauzi bin Ridwan",
            StudentNisn: "0012345678",
            AcademicPeriodId: academicPeriodId,
            PeriodLabel: "Semester Ganjil 2026/2027",
            ReportType: "end_of_term",
            TemplateName: "Buku Rapor Merdeka",
            Status: "published",
            ApprovedBy: "kepala.sekolah@elsasa.sch.id",
            ApprovedDate: approvedDate,
            NarrativeComments: "Siswa menunjukkan pertumbuhan karakter mandiri, gotong royong, dan integritas tinggi.",
            SubjectEntries: [subject1, subject2],
            AttendanceSummary: attendance);

        // Act
        string json = JsonSerializer.Serialize(originalSnapshot, _webOptions);
        var deserializedSnapshot = JsonSerializer.Deserialize<ReportCardSnapshotDto>(json, _webOptions);

        // Assert
        deserializedSnapshot.Should().NotBeNull();
        deserializedSnapshot.Should().BeEquivalentTo(originalSnapshot);
        json.Should().Contain("\"reportCardId\":");
        json.Should().Contain("\"studentName\":\"Ahmad Fauzi bin Ridwan\"");
        json.Should().Contain("\"scoreNumeric\":87.50");
    }

    [Fact]
    public void ReportCardSnapshot_Should_RoundTrip_Successfully_With_Null_Optional_Fields()
    {
        // Arrange
        var originalSnapshot = new ReportCardSnapshotDto(
            ReportCardId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            StudentName: "Siti Rahma",
            StudentNisn: null,
            AcademicPeriodId: Guid.NewGuid(),
            PeriodLabel: "Mid Term 1 2026",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            Status: "approved",
            ApprovedBy: null,
            ApprovedDate: null,
            NarrativeComments: null,
            SubjectEntries: [],
            AttendanceSummary: null);

        // Act
        string json = JsonSerializer.Serialize(originalSnapshot, _webOptions);
        var deserializedSnapshot = JsonSerializer.Deserialize<ReportCardSnapshotDto>(json, _webOptions);

        // Assert
        deserializedSnapshot.Should().NotBeNull();
        deserializedSnapshot.Should().BeEquivalentTo(originalSnapshot);
        deserializedSnapshot!.StudentNisn.Should().BeNull();
        deserializedSnapshot.ApprovedBy.Should().BeNull();
        deserializedSnapshot.ApprovedDate.Should().BeNull();
        deserializedSnapshot.NarrativeComments.Should().BeNull();
        deserializedSnapshot.AttendanceSummary.Should().BeNull();
        deserializedSnapshot.SubjectEntries.Should().BeEmpty();
    }

    [Fact]
    public void ReportCardSnapshot_Should_Preserve_Exact_Decimal_Precision_And_Indonesian_Diacritics()
    {
        // Arrange
        var subject = new ReportCardSubjectEntryDto(
            LearningUnitId: Guid.NewGuid(),
            SubjectCode: "AGM-01",
            SubjectTitle: "Pendidikan Agama & Budi Pekerti (التربية)",
            ScoreNumeric: 99.99m,
            GradeLetter: "A*",
            GradeLabel: "Mumtaz",
            TeacherComment: "Hafalan Juz 30 sangat lancar; makharijul huruf fasih & tartil.",
            DisplayOrder: 1);

        var original = new ReportCardSnapshotDto(
            ReportCardId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            StudentName: "Nurul Aini Syifa' Binti Zulkifli",
            StudentNisn: "0098765432",
            AcademicPeriodId: Guid.NewGuid(),
            PeriodLabel: "Semester 2 2026/2027",
            ReportType: "end_of_term",
            TemplateName: "Buku Rapor Merdeka",
            Status: "published",
            ApprovedBy: "authority@elsasa.sch.id",
            ApprovedDate: DateTime.UtcNow,
            NarrativeComments: "Capaian akademis istimewa.",
            SubjectEntries: [subject],
            AttendanceSummary: new ReportCardAttendanceSummaryDto(100, 0, 0, 0));

        // Act
        string json = JsonSerializer.Serialize(original, _webOptions);
        var deserialized = JsonSerializer.Deserialize<ReportCardSnapshotDto>(json, _webOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.SubjectEntries[0].ScoreNumeric.Should().Be(99.99m);
        deserialized.StudentName.Should().Be("Nurul Aini Syifa' Binti Zulkifli");
        deserialized.SubjectEntries[0].SubjectTitle.Should().Contain("(التربية)");
    }

    #endregion

    #region TranscriptSnapshotDto Serialization Tests

    [Fact]
    public void TranscriptSnapshot_Should_RoundTrip_Successfully_With_Full_Payload()
    {
        // Arrange
        var transcriptId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var period1Id = Guid.NewGuid();
        var period2Id = Guid.NewGuid();

        var course1 = new TranscriptCourseEntryDto(
            LearningUnitId: Guid.NewGuid(),
            SubjectCode: "BIO-101",
            SubjectTitle: "Biologi Umum",
            Credits: 3.0m,
            ScoreNumeric: 89.0m,
            GradeLetter: "A",
            IsTransferred: false);

        var course2 = new TranscriptCourseEntryDto(
            LearningUnitId: Guid.NewGuid(),
            SubjectCode: "CHM-101",
            SubjectTitle: "Kimia Dasar",
            Credits: 3.0m,
            ScoreNumeric: 82.5m,
            GradeLetter: "B+",
            IsTransferred: true); // Transfer credit flag [T]

        var periodSummary1 = new TranscriptPeriodSummaryDto(
            AcademicPeriodId: period1Id,
            PeriodLabel: "Tahun Ajaran 2025/2026 Semester 1",
            Courses: [course1, course2],
            Gpa: 3.65m);

        var course3 = new TranscriptCourseEntryDto(
            LearningUnitId: Guid.NewGuid(),
            SubjectCode: "PHY-101",
            SubjectTitle: "Fisika Mekanika",
            Credits: 4.0m,
            ScoreNumeric: 95.0m,
            GradeLetter: "A",
            IsTransferred: false);

        var periodSummary2 = new TranscriptPeriodSummaryDto(
            AcademicPeriodId: period2Id,
            PeriodLabel: "Tahun Ajaran 2025/2026 Semester 2",
            Courses: [course3],
            Gpa: 4.00m);

        var originalTranscript = new TranscriptSnapshotDto(
            TranscriptId: transcriptId,
            StudentId: studentId,
            StudentName: "Dewi Lestari",
            StudentNisn: "0055443322",
            TranscriptType: "official",
            PeriodStart: new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Status: "issued",
            Purpose: "Pendaftaran Beasiswa Pendidikan Tinggi",
            CompletedPeriods: [periodSummary1, periodSummary2]);

        // Act
        string json = JsonSerializer.Serialize(originalTranscript, _webOptions);
        var deserializedTranscript = JsonSerializer.Deserialize<TranscriptSnapshotDto>(json, _webOptions);

        // Assert
        deserializedTranscript.Should().NotBeNull();
        deserializedTranscript.Should().BeEquivalentTo(originalTranscript);
        deserializedTranscript!.CompletedPeriods.Should().HaveCount(2);
        deserializedTranscript.CompletedPeriods[0].Courses[1].IsTransferred.Should().BeTrue();
        deserializedTranscript.CompletedPeriods[1].Gpa.Should().Be(4.00m);
    }

    [Fact]
    public void TranscriptSnapshot_Should_RoundTrip_Successfully_With_Minimal_Payload()
    {
        // Arrange
        var originalTranscript = new TranscriptSnapshotDto(
            TranscriptId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            StudentName: "Budi Santoso",
            StudentNisn: null,
            TranscriptType: "unofficial",
            PeriodStart: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            Status: "draft",
            Purpose: null,
            CompletedPeriods: []);

        // Act
        string json = JsonSerializer.Serialize(originalTranscript, _webOptions);
        var deserialized = JsonSerializer.Deserialize<TranscriptSnapshotDto>(json, _webOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().BeEquivalentTo(originalTranscript);
        deserialized!.StudentNisn.Should().BeNull();
        deserialized.Purpose.Should().BeNull();
        deserialized.CompletedPeriods.Should().BeEmpty();
    }

    [Fact]
    public void Deserialization_Should_Throw_JsonException_On_Malformed_Json()
    {
        // Arrange
        string malformedJson = "{ \"reportCardId\": \"invalid-guid-value\", \"studentId\": 123 }";

        // Act
        var act = () => JsonSerializer.Deserialize<ReportCardSnapshotDto>(malformedJson, _webOptions);

        // Assert
        act.Should().Throw<JsonException>();
    }

    #endregion
}
