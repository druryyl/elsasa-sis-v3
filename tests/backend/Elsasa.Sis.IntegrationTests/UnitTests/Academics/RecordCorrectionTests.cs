namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.RecordCorrections;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;
using Elsasa.Sis.Contracts.Academics.Events;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for auditable record corrections slices (UC-ACD-19, TD-06, P6-S14).
/// Tests <see cref="CorrectApprovedResultValidator"/>, request property aliases, event contracts, and DTOs.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class RecordCorrectionTests
{
    private readonly CorrectApprovedResultValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_Should_Fail_When_Reason_Is_Empty(string? reason)
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 85m,
            Reason: reason!);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Theory]
    [InlineData("Too short")]
    [InlineData("123456789")]
    [InlineData("Typo")]
    public void Validator_Should_Fail_When_Reason_Is_Less_Than_10_Characters(string shortReason)
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 85m,
            Reason: shortReason);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage.Contains("at least 10 characters"));
    }

    [Fact]
    public void Validator_Should_Fail_When_No_Correction_Field_Is_Provided()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            Reason: "Recalculated arithmetic grade following teacher review");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("At least one corrected value field"));
    }

    [Fact]
    public void Validator_Should_Fail_When_ScoreNumeric_Is_Negative()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: -5m,
            Reason: "Grade corrected after student appeal");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("greater than or equal to 0"));
    }

    [Fact]
    public void Validator_Should_Fail_When_ScoreNumeric_Exceeds_ScoreMax()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 105m,
            ScoreMax: 100m,
            Reason: "Grade corrected after student appeal");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot exceed corrected maximum score"));
    }

    [Fact]
    public void Validator_Should_Fail_When_ScoreMax_Is_Zero_Or_Negative()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 10m,
            ScoreMax: 0m,
            Reason: "Grade corrected after student appeal");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("greater than 0"));
    }

    [Fact]
    public void Validator_Should_Fail_When_GradeLetter_Exceeds_10_Characters()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            GradeLetter: new string('A', 11),
            Reason: "Grade corrected after student appeal");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("not exceed 10 characters"));
    }

    [Fact]
    public void Validator_Should_Fail_When_GradeLabel_Exceeds_100_Characters()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            GradeLabel: new string('L', 101),
            Reason: "Grade corrected after student appeal");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("not exceed 100 characters"));
    }

    [Fact]
    public void Validator_Should_Fail_When_Evidence_Exceeds_2000_Characters()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 90m,
            Evidence: new string('E', 2001),
            Reason: "Grade corrected after student appeal");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("not exceed 2000 characters"));
    }

    [Fact]
    public void Validator_Should_Succeed_When_All_Fields_Are_Valid()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 88.5m,
            ScoreMax: 100m,
            GradeLetter: "A",
            GradeLabel: "Sangat Baik",
            NarrativeFeedback: "Exemplary progress demonstrated in remediation coursework.",
            Reason: "Scoring rubric item 3 recalculated after committee moderation.",
            Evidence: "https://documents.elsasa.sch.id/corrections/REQ-2026-004.pdf",
            IsMaterial: true);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Request_Should_Support_NewField_Aliases_And_EffectiveProperties()
    {
        // Arrange
        var request = new CorrectApprovedResultRequest(
            NewScoreNumeric: 92.0m,
            NewScoreMax: 100.0m,
            NewGradeLetter: "A+",
            NewGradeLabel: "Istimewa",
            NewNarrativeFeedback: "Mastery achieved across all learning objectives.",
            Reason: "Re-evaluation after submission remarking",
            IsMaterial: false);

        // Assert
        request.EffectiveScoreNumeric.Should().Be(92.0m);
        request.EffectiveScoreMax.Should().Be(100.0m);
        request.EffectiveGradeLetter.Should().Be("A+");
        request.EffectiveGradeLabel.Should().Be("Istimewa");
        request.EffectiveNarrativeFeedback.Should().Be("Mastery achieved across all learning objectives.");
        request.HasAnyCorrection.Should().BeTrue();

        var validationResult = _validator.Validate(request);
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AcademicResultCorrectedEvent_Should_Map_Properties_Properly()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var reason = "Correction of calculation discrepancy";
        var isMaterial = true;
        var correctedBy = "authority.user";

        // Act
        var evt = new AcademicResultCorrectedEvent(
            resultId,
            assessmentId,
            studentId,
            reason,
            isMaterial,
            correctedBy);

        // Assert
        evt.ResultId.Should().Be(resultId);
        evt.AssessmentId.Should().Be(assessmentId);
        evt.StudentId.Should().Be(studentId);
        evt.Reason.Should().Be(reason);
        evt.IsMaterial.Should().BeTrue();
        evt.CorrectedBy.Should().Be(correctedBy);
        evt.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AcademicRecordCorrectionDto_Should_Retain_Constructor_Values()
    {
        // Arrange
        var id = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        // Act
        var dto = new AcademicRecordCorrectionDto(
            Id: id,
            RecordType: "academic_result",
            RecordId: recordId,
            OriginalValue: "{\"score_numeric\": 75.00}",
            CorrectedValue: "{\"score_numeric\": 85.00}",
            Reason: "Summative grade adjustment after recheck",
            Evidence: "DOC-REF-0123",
            CorrectedBy: "academic.authority",
            CorrectedDate: today,
            IsMaterial: true,
            Notes: "Verified with department head",
            CreatedAt: now,
            CreatedBy: "academic.authority");

        // Assert
        dto.Id.Should().Be(id);
        dto.RecordType.Should().Be("academic_result");
        dto.RecordId.Should().Be(recordId);
        dto.OriginalValue.Should().Contain("75.00");
        dto.CorrectedValue.Should().Contain("85.00");
        dto.Reason.Should().Be("Summative grade adjustment after recheck");
        dto.Evidence.Should().Be("DOC-REF-0123");
        dto.CorrectedBy.Should().Be("academic.authority");
        dto.CorrectedDate.Should().Be(today);
        dto.IsMaterial.Should().BeTrue();
        dto.Notes.Should().Be("Verified with department head");
        dto.CreatedAt.Should().Be(now);
        dto.CreatedBy.Should().Be("academic.authority");
    }
}
