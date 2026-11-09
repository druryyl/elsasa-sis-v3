namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.CompileReportCard;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="BatchCompileCohortReportCardsValidator"/> and <see cref="CompileReportCardValidator"/>
/// verifying period requirement, report type enumeration, template constraints, and section filters (UC-ACD-13, ACD-WF-12, TD-07, P5-S11, P7-S16).
/// </summary>
[Trait("Category", "UnitTest")]
[Trait("Category", "Unit")]
public sealed class ReportCardBatchCompileValidatorTests
{
    private readonly BatchCompileCohortReportCardsValidator _batchValidator = new();
    private readonly CompileReportCardValidator _compileValidator = new();

    #region BatchCompileCohortReportCardsValidator Tests

    [Theory]
    [InlineData("mid_term", "Grade 4-A")]
    [InlineData("end_of_term", "Kelas 10-MIPA-1")]
    [InlineData("mid_term", null)]
    public void BatchValidator_Should_Pass_When_Request_Is_Valid(string reportType, string? sectionLabel)
    {
        // Arrange
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester Ganjil 2026/2027",
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
            AcademicPeriodLabel: "Semester Ganjil 2026/2027",
            ReportType: "end_of_term",
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: null);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.AcademicPeriodId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BatchValidator_Should_Fail_When_AcademicPeriodLabel_Is_Blank(string? label)
    {
        // Arrange
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: label!,
            ReportType: "end_of_term",
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: null);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.AcademicPeriodLabel));
    }

    [Theory]
    [InlineData("annual")]
    [InlineData("quarterly")]
    [InlineData("unknown_type")]
    public void BatchValidator_Should_Fail_When_ReportType_Is_Invalid(string invalidReportType)
    {
        // Arrange
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester Ganjil 2026/2027",
            ReportType: invalidReportType,
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: null);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ReportType));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BatchValidator_Should_Fail_When_TemplateName_Is_Blank(string? template)
    {
        // Arrange
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester Ganjil 2026/2027",
            ReportType: "end_of_term",
            TemplateName: template!,
            SectionLabel: null);

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.TemplateName));
    }

    [Fact]
    public void BatchValidator_Should_Fail_When_SectionLabel_Exceeds_MaxLength()
    {
        // Arrange: 51 characters exceeds max 50
        var request = new BatchCompileCohortReportCardsRequest(
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester Ganjil 2026/2027",
            ReportType: "end_of_term",
            TemplateName: "Buku Rapor Merdeka",
            SectionLabel: new string('S', 51));

        // Act
        var result = _batchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.SectionLabel));
    }

    #endregion

    #region CompileReportCardValidator Tests

    [Fact]
    public void CompileValidator_Should_Pass_When_Request_Is_Valid()
    {
        // Arrange
        var request = new CompileReportCardRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester Ganjil 2026/2027",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            NarrativeComments: "Siswa aktif dan rajin.");

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
            AcademicPeriodLabel: "Semester Ganjil 2026/2027",
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
    public void CompileValidator_Should_Fail_When_NarrativeComments_Exceeds_4000_Characters()
    {
        // Arrange
        var request = new CompileReportCardRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "Semester Ganjil 2026/2027",
            ReportType: "mid_term",
            TemplateName: "Buku Rapor Merdeka",
            NarrativeComments: new string('A', 4001));

        // Act
        var result = _compileValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.NarrativeComments));
    }

    #endregion
}
