namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.AssessmentEvidence.RecordEvidence;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="RecordEvidenceValidator"/> (P3-S06, ACD-WF-08, ACD-WF-20, UC-ACD-07, GAP-07).
/// </summary>
[Trait("Category", "Unit")]
public sealed class AssessmentEvidenceValidatorTests
{
    private readonly RecordEvidenceValidator _validator = new();

    [Fact]
    public void Validate_Should_Succeed_When_StudentEvidenceIsValid()
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            SubmissionId: Guid.NewGuid(),
            EvidenceType: "test_paper",
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: "https://storage.elsasa.sch.id/evidence/test-01.pdf",
            Notes: "Student completed written midterm paper.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Should_Succeed_When_ApplicantEvidenceIsValid()
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: null,
            ApplicantId: Guid.NewGuid(),
            SubmissionId: null,
            EvidenceType: "observation",
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EvidenceReference: null,
            Notes: "Admissions entrance diagnostic observation note.");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("test_paper")]
    [InlineData("oral_response")]
    [InlineData("project_artifact")]
    [InlineData("portfolio_item")]
    [InlineData("observation")]
    [InlineData("practical_demonstration")]
    [InlineData("other")]
    [InlineData("TEST_PAPER")]
    [InlineData("  Project_Artifact  ")]
    public void Validate_Should_Succeed_For_AllAllowedEvidenceTypes(string evidenceType)
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            SubmissionId: null,
            EvidenceType: evidenceType,
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: null,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_BothStudentAndApplicantAreProvided()
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: Guid.NewGuid(),
            SubmissionId: null,
            EvidenceType: "test_paper",
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: null,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Exactly one of student_id or applicant_id"));
    }

    [Fact]
    public void Validate_Should_Fail_When_NeitherStudentNorApplicantIsProvided()
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: null,
            ApplicantId: null,
            SubmissionId: null,
            EvidenceType: "test_paper",
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: null,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Exactly one of student_id or applicant_id"));
    }

    [Fact]
    public void Validate_Should_Fail_When_BothIdsAreEmptyGuids()
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.Empty,
            ApplicantId: Guid.Empty,
            SubmissionId: null,
            EvidenceType: "test_paper",
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: null,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Exactly one of student_id or applicant_id"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_EvidenceTypeIsMissing(string? evidenceType)
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            SubmissionId: null,
            EvidenceType: evidenceType!,
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: null,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.EvidenceType));
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData("exam_paper")]
    [InlineData("video_recording")]
    public void Validate_Should_Fail_When_EvidenceTypeIsInvalid(string invalidType)
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            SubmissionId: null,
            EvidenceType: invalidType,
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: null,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.EvidenceType));
    }

    [Fact]
    public void Validate_Should_Fail_When_CollectedDateIsInTheFuture()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            SubmissionId: null,
            EvidenceType: "test_paper",
            CollectedDate: futureDate,
            EvidenceReference: null,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CollectedDate) && e.ErrorMessage.Contains("future"));
    }

    [Fact]
    public void Validate_Should_Fail_When_EvidenceReferenceExceeds500Characters()
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            SubmissionId: null,
            EvidenceType: "test_paper",
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: new string('A', 501),
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.EvidenceReference));
    }

    [Fact]
    public void Validate_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            SubmissionId: null,
            EvidenceType: "test_paper",
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: null,
            Notes: new string('B', 4001));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Notes));
    }

    [Fact]
    public void Validate_Should_Fail_When_SubmissionIdIsEmptyGuid()
    {
        // Arrange
        var request = new RecordEvidenceRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            SubmissionId: Guid.Empty,
            EvidenceType: "test_paper",
            CollectedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EvidenceReference: null,
            Notes: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.SubmissionId));
    }
}
