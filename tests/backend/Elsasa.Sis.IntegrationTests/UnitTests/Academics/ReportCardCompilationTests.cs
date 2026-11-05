namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.CompileReportCard;
using Elsasa.Sis.Shared.Common;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for report cards compilation and batch cohort compilation slices (UC-ACD-13, ACD-WF-12, TD-07, P5-S11).
/// Verifies validation rules, response DTOs, and domain error definitions.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class ReportCardCompilationTests
{
    private readonly CompileReportCardValidator _compileValidator = new();
    private readonly BatchCompileCohortReportCardsValidator _batchValidator = new();

    [Theory]
    [InlineData("mid_term")]
    [InlineData("end_of_term")]
    public void CompileValidator_Should_Pass_For_Valid_ReportTypes(string reportType)
    {
        // Arrange
        var request = new CompileReportCardRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1 2026/2027",
            ReportType: reportType,
            TemplateName: "Buku Rapor Merdeka",
            NarrativeComments: "Excellent progress throughout the term.");

        // Act
        var result = _compileValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CompileValidator_Should_Fail_When_StudentId_Is_Empty()
    {
        // Arrange
        var request = new CompileReportCardRequest(
            StudentId: Guid.Empty,
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            NarrativeComments: null);

        // Act
        var result = _compileValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.StudentId));
    }

    [Fact]
    public void CompileValidator_Should_Fail_When_AcademicPeriodId_Is_Empty()
    {
        // Arrange
        var request = new CompileReportCardRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.Empty,
            AcademicPeriodLabel: "Semester 1",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            NarrativeComments: null);

        // Act
        var result = _compileValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.AcademicPeriodId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CompileValidator_Should_Fail_When_AcademicPeriodLabel_Is_Blank(string? label)
    {
        // Arrange
        var request = new CompileReportCardRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: label!,
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            NarrativeComments: null);

        // Act
        var result = _compileValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.AcademicPeriodLabel));
    }

    [Theory]
    [InlineData("annual")]
    [InlineData("quarterly")]
    [InlineData("invalid_type")]
    public void CompileValidator_Should_Fail_When_ReportType_Is_Invalid(string invalidType)
    {
        // Arrange
        var request = new CompileReportCardRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1",
            ReportType: invalidType,
            TemplateName: "Buku Rapor Merdeka",
            NarrativeComments: null);

        // Act
        var result = _compileValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ReportType));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CompileValidator_Should_Fail_When_TemplateName_Is_Blank(string? template)
    {
        // Arrange
        var request = new CompileReportCardRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1",
            ReportType: "mid_term",
            TemplateName: template!,
            NarrativeComments: null);

        // Act
        var result = _compileValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.TemplateName));
    }

    [Fact]
    public void CompileValidator_Should_Fail_When_NarrativeComments_Exceeds_MaxLength()
    {
        // Arrange
        var longNarrative = new string('A', 4001);
        var request = new CompileReportCardRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester 1",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            NarrativeComments: longNarrative);

        // Act
        var result = _compileValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.NarrativeComments));
    }

    [Theory]
    [InlineData("mid_term", "4A")]
    [InlineData("end_of_term", null)]
    [InlineData("mid_term", "Grade 7-B")]
    public void BatchValidator_Should_Pass_For_Valid_Requests(string reportType, string? sectionLabel)
    {
        // Arrange
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Term 1 2026/2027",
            ReportType: reportType,
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: sectionLabel);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_AcademicPeriodId_Is_Empty()
    {
        // Arrange
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.Empty,
            AcademicPeriodLabel: "Term 1",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: "4A");

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.AcademicPeriodId));
    }

    [Theory]
    [InlineData("weekly")]
    [InlineData("semester_final")]
    public void BatchValidator_Should_Fail_When_ReportType_Is_Invalid(string invalidType)
    {
        // Arrange
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Term 1",
            ReportType: invalidType,
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: "4A");

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ReportType));
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_SectionLabel_Exceeds_MaxLength()
    {
        // Arrange
        var longSection = new string('S', 51);
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Term 1",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: longSection);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.SectionLabel));
    }

    [Fact]
    public void ReportCardDto_Should_Map_And_Preserve_All_Properties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var entries = new List<ReportCardEntryDto>
        {
            new(
                entryId,
                id,
                unitId,
                "MAT-101",
                "Mathematics",
                resultId,
                88.50m,
                "A",
                "Kompeten",
                1,
                "Shows great problem solving ability.")
        };

        // Act
        var dto = new ReportCardDto(
            id,
            studentId,
            periodId,
            "Semester 1 2026/2027",
            "mid_term",
            "Buku Rapor Merdeka",
            "draft",
            null,
            null,
            null,
            "Good overall conduct.",
            null,
            entries,
            now,
            "teacher1");

        // Assert
        dto.Id.Should().Be(id);
        dto.StudentId.Should().Be(studentId);
        dto.AcademicPeriodId.Should().Be(periodId);
        dto.AcademicPeriodLabel.Should().Be("Semester 1 2026/2027");
        dto.ReportType.Should().Be("mid_term");
        dto.TemplateName.Should().Be("Buku Rapor Merdeka");
        dto.Status.Should().Be("draft");
        dto.ApprovedBy.Should().BeNull();
        dto.ApprovedDate.Should().BeNull();
        dto.PublishedDate.Should().BeNull();
        dto.NarrativeComments.Should().Be("Good overall conduct.");
        dto.ContentSnapshot.Should().BeNull();
        dto.Entries.Should().HaveCount(1);
        dto.Entries[0].SubjectCode.Should().Be("MAT-101");
        dto.Entries[0].ScoreNumeric.Should().Be(88.50m);
        dto.Entries[0].GradeLetter.Should().Be("A");
        dto.Entries[0].GradeLabel.Should().Be("Kompeten");
        dto.CreatedAt.Should().Be(now);
        dto.CreatedBy.Should().Be("teacher1");
    }

    [Fact]
    public void BatchCompilationSummaryDto_Should_Map_Counts_Correctly()
    {
        // Arrange & Act
        var summary = new BatchCompilationSummaryDto(25, 3);

        // Assert
        summary.CompiledCount.Should().Be(25);
        summary.SkippedExistingCount.Should().Be(3);
    }

    [Fact]
    public void ReportCards_DomainErrors_Should_Have_Expected_Codes_And_Types()
    {
        // Assert
        DomainErrors.Academics.ReportCards.AlreadyExists.Code.Should().Be("Academics.ReportCards.AlreadyExists");
        DomainErrors.Academics.ReportCards.AlreadyExists.Type.Should().Be(ErrorType.Conflict);

        DomainErrors.Academics.ReportCards.NoActiveParticipations.Code.Should().Be("Academics.ReportCards.NoActiveParticipations");
        DomainErrors.Academics.ReportCards.NoActiveParticipations.Type.Should().Be(ErrorType.NotFound);

        DomainErrors.Academics.ReportCards.NotFound.Code.Should().Be("Academics.ReportCards.NotFound");
        DomainErrors.Academics.ReportCards.NotFound.Type.Should().Be(ErrorType.NotFound);
    }
}
