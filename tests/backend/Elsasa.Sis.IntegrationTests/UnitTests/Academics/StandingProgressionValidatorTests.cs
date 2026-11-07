namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.AcademicInterventions.InitiateAcademicIntervention;
using Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;
using Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;
using Elsasa.Sis.Contracts.Academics;
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

/// <summary>
/// Unit test suite for Standing &amp; Progression FluentValidation request validators,
/// record corrections reason guards, and Submodule 4 DTO mappings (P6-S15, UC-ACD-11, UC-ACD-12, UC-ACD-15, UC-ACD-17).
/// </summary>
[Trait("Category", "UnitTest")]
[Trait("Category", "Unit")]
public sealed class StandingProgressionValidatorTests
{
    private readonly RecordLearningProgressValidator _progressValidator = new();
    private readonly UpdateInterventionStatusRequestValidator _interventionStatusRequestValidator = new();
    private readonly UpdateInterventionStatusCommandValidator _interventionStatusCommandValidator = new();
    private readonly InitiateAcademicInterventionValidator _interventionInitiateValidator = new();
    private readonly CorrectApprovedResultValidator _resultCorrectionValidator = new();

    #region 1. Progress Status and Basis Enumeration Constraints

    [Theory]
    [InlineData("not_started")]
    [InlineData("in_progress")]
    [InlineData("completed")]
    [InlineData("needs_remediation")]
    [InlineData("exempted")]
    [InlineData("NOT_STARTED")]
    [InlineData("In_Progress")]
    public void ProgressValidator_AllowedStatuses_ShouldPassValidation(string status)
    {
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: status,
            Basis: "assessment_evidence");

        var result = _progressValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("passed")]
    [InlineData("failed")]
    [InlineData("withdrawn")]
    [InlineData("deferred")]
    public void ProgressValidator_DisallowedStatus_ShouldFailValidation(string invalidStatus)
    {
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: invalidStatus,
            Basis: "assessment_evidence");

        var result = _progressValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("assessment_evidence")]
    [InlineData("learning_work")]
    [InlineData("authorized_decision")]
    [InlineData("ASSESSMENT_EVIDENCE")]
    public void ProgressValidator_AllowedBases_ShouldPassValidation(string basis)
    {
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "in_progress",
            Basis: basis,
            Notes: "Explanatory notes for authorized decision");

        var result = _progressValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Basis);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("attendance")]
    [InlineData("teacher_opinion")]
    [InlineData("manual_override")]
    public void ProgressValidator_DisallowedBasis_ShouldFailValidation(string invalidBasis)
    {
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "in_progress",
            Basis: invalidBasis);

        var result = _progressValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Basis);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProgressValidator_WhenBasisIsAuthorizedDecision_RequiresNotes(string? notes)
    {
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "completed",
            Basis: "authorized_decision",
            Notes: notes);

        var result = _progressValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Explanatory notes are required when basis is 'authorized_decision'.");
    }

    [Fact]
    public void ProgressValidator_WhenBasisIsAssessmentEvidence_NotesAreOptional()
    {
        var command = new RecordLearningProgressCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Status: "completed",
            Basis: "assessment_evidence",
            Notes: null);

        var result = _progressValidator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    #endregion

    #region 2. Academic Standing Determination Validation Rules

    [Theory]
    [InlineData("good_standing")]
    [InlineData("conditional_standing")]
    [InlineData("not_in_good_standing")]
    public void StandingValidator_AllowedStandings_ShouldPassEnumerationCheck(string standing)
    {
        var validator = new TestDetermineAcademicStandingRequestValidator();
        var request = new TestDetermineAcademicStandingRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: standing,
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: standing == "conditional_standing" ? "Must pass remedial math packet" : null,
            IsException: false);

        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Standing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("probation")]
    [InlineData("honors")]
    [InlineData("expelled")]
    public void StandingValidator_DisallowedStanding_ShouldFailEnumerationCheck(string invalidStanding)
    {
        var validator = new TestDetermineAcademicStandingRequestValidator();
        var request = new TestDetermineAcademicStandingRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: invalidStanding,
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: false);

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Standing);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StandingValidator_WhenStandingIsConditional_MandatoryConditionsRequired(string? conditions)
    {
        var validator = new TestDetermineAcademicStandingRequestValidator();
        var request = new TestDetermineAcademicStandingRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "conditional_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: conditions,
            IsException: false);

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Conditions)
            .WithErrorMessage("Conditions are mandatory when standing is 'conditional_standing'.");
    }

    [Fact]
    public void StandingValidator_WhenStandingIsGoodStanding_ConditionsAreOptional()
    {
        var validator = new TestDetermineAcademicStandingRequestValidator();
        var request = new TestDetermineAcademicStandingRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: false);

        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Conditions);
    }

    [Fact]
    public void StandingValidator_WhenIsExceptionIsTrue_RequiresReasonAndCommitteeReference()
    {
        var validator = new TestDetermineAcademicStandingRequestValidator();
        var request = new TestDetermineAcademicStandingRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: true,
            ExceptionReason: null,
            CommitteeReference: null);

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    [Fact]
    public void StandingValidator_WhenIsExceptionIsTrue_ShortReasonOrReferenceFails()
    {
        var validator = new TestDetermineAcademicStandingRequestValidator();
        var request = new TestDetermineAcademicStandingRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: true,
            ExceptionReason: "Four", // < 5 chars
            CommitteeReference: "SK"); // < 5 chars

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    [Fact]
    public void StandingValidator_WhenIsExceptionIsTrue_ValidReasonAndReferencePasses()
    {
        var validator = new TestDetermineAcademicStandingRequestValidator();
        var request = new TestDetermineAcademicStandingRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: true,
            ExceptionReason: "Authorized by Academic Council decree following medical leave",
            CommitteeReference: "SK Dewan Guru No. 12/2026");

        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldNotHaveValidationErrorFor(x => x.CommitteeReference);
    }

    [Fact]
    public void StandingValidator_WhenIsExceptionIsFalse_ReasonAndReferenceNotRequired()
    {
        var validator = new TestDetermineAcademicStandingRequestValidator();
        var request = new TestDetermineAcademicStandingRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null);

        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldNotHaveValidationErrorFor(x => x.CommitteeReference);
    }

    #endregion

    #region 3. Promotion Recommendation Issuance Validation Rules

    [Theory]
    [InlineData("promotion_eligible")]
    [InlineData("conditional_promotion")]
    [InlineData("retention_recommended")]
    public void ProgressionValidator_AllowedRecommendations_ShouldPassEnumerationCheck(string recommendation)
    {
        var validator = new TestIssuePromotionRecommendationRequestValidator();
        var request = new TestIssuePromotionRecommendationRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: recommendation,
            FromLevel: "Grade 4",
            RecommendedToLevel: recommendation == "retention_recommended" ? null : "Grade 5",
            Conditions: recommendation == "conditional_promotion" ? "Complete Math summer packet" : null,
            SupportingEvidence: "5 of 5 learning units passed with 92% attendance",
            IsException: false);

        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Recommendation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("graduated")]
    [InlineData("promoted_with_honors")]
    [InlineData("skip_grade")]
    public void ProgressionValidator_DisallowedRecommendation_ShouldFailEnumerationCheck(string invalidRecommendation)
    {
        var validator = new TestIssuePromotionRecommendationRequestValidator();
        var request = new TestIssuePromotionRecommendationRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: invalidRecommendation,
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            Conditions: null,
            SupportingEvidence: "Evidence here",
            IsException: false);

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Recommendation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProgressionValidator_WhenConditionalPromotion_RequiresMandatoryConditions(string? conditions)
    {
        var validator = new TestIssuePromotionRecommendationRequestValidator();
        var request = new TestIssuePromotionRecommendationRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "conditional_promotion",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            Conditions: conditions,
            SupportingEvidence: "4 of 5 units passed",
            IsException: false);

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Conditions)
            .WithErrorMessage("Conditions are mandatory when recommendation is 'conditional_promotion'.");
    }

    [Fact]
    public void ProgressionValidator_WhenIsExceptionIsTrue_RequiresReasonAndCommitteeReference()
    {
        var validator = new TestIssuePromotionRecommendationRequestValidator();
        var request = new TestIssuePromotionRecommendationRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            Conditions: null,
            SupportingEvidence: "Supported by committee review",
            IsException: true,
            ExceptionReason: null,
            CommitteeReference: null);

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldHaveValidationErrorFor(x => x.CommitteeReference);
    }

    [Fact]
    public void ProgressionValidator_WhenIsExceptionIsTrue_ValidDetailsPass()
    {
        var validator = new TestIssuePromotionRecommendationRequestValidator();
        var request = new TestIssuePromotionRecommendationRequest(
            StudentId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            Conditions: null,
            SupportingEvidence: "Supported by committee review",
            IsException: true,
            ExceptionReason: "Overriding failed attendance due to documented hospital stay",
            CommitteeReference: "SK Dewan Guru No. 15/2026");

        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ExceptionReason);
        result.ShouldNotHaveValidationErrorFor(x => x.CommitteeReference);
    }

    #endregion

    #region 4. Auditable Record Corrections Reason Length (Min 10 Chars)

    [Theory]
    [InlineData("1234567890", true)]
    [InlineData("Koreksi status kelulusan siswa sesuai SK dewan guru", true)]
    [InlineData("Correction of typo in policy reference", true)]
    [InlineData("123456789", false)]
    [InlineData("Typo", false)]
    [InlineData("Fixed", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void RecordCorrection_MandatoryReason_Minimum10CharsEnforced(string? reason, bool expectedValid)
    {
        var request = new CorrectApprovedResultRequest(
            ScoreNumeric: 85m,
            Reason: reason!);

        var result = _resultCorrectionValidator.Validate(request);

        if (expectedValid)
        {
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
        else
        {
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Reason));
        }
    }

    [Fact]
    public void RecordCorrection_StandingCorrectionRequestValidator_EnforcesMin10Chars()
    {
        var validator = new TestCorrectAcademicRecordRequestValidator();

        var invalidRequest = new TestCorrectAcademicRecordRequest(
            RecordId: Guid.NewGuid(),
            NewValue: "good_standing",
            Reason: "Short"); // 5 chars < 10

        var invalidResult = validator.TestValidate(invalidRequest);
        invalidResult.ShouldHaveValidationErrorFor(x => x.Reason);

        var validRequest = new TestCorrectAcademicRecordRequest(
            RecordId: Guid.NewGuid(),
            NewValue: "good_standing",
            Reason: "Corrected following principal approval decree"); // > 10 chars

        var validResult = validator.TestValidate(validRequest);
        validResult.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    #endregion

    #region 5. Academic Intervention Lifecycle Status & Outcome Notes

    [Fact]
    public void InterventionStatusValidator_WhenCompletedWithoutOutcomeNotes_ShouldFailValidation()
    {
        var request = new UpdateInterventionStatusRequest(
            Status: "completed",
            OutcomeNotes: null);

        var result = _interventionStatusRequestValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.OutcomeNotes)
            .WithErrorMessage("Outcome notes are required when completing an intervention.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Done")] // < 5 characters
    public void InterventionStatusValidator_WhenCompletedWithShortOutcomeNotes_ShouldFailValidation(string? notes)
    {
        var request = new UpdateInterventionStatusRequest(
            Status: "completed",
            OutcomeNotes: notes);

        var result = _interventionStatusRequestValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.OutcomeNotes);
    }

    [Fact]
    public void InterventionStatusValidator_WhenCompletedWithValidOutcomeNotes_ShouldPassValidation()
    {
        var request = new UpdateInterventionStatusRequest(
            Status: "completed",
            OutcomeNotes: "Student completed 4 remedial sessions and achieved 80% on re-assessment.");

        var result = _interventionStatusRequestValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.OutcomeNotes);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("planned")]
    [InlineData("in_progress")]
    [InlineData("cancelled")]
    public void InterventionStatusValidator_WhenNotCompleted_OutcomeNotesAreOptional(string status)
    {
        var request = new UpdateInterventionStatusRequest(
            Status: status,
            OutcomeNotes: null);

        var result = _interventionStatusRequestValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.OutcomeNotes);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void InterventionStatusCommandValidator_WhenCompletedWithoutOutcomeNotes_ShouldFailValidation()
    {
        var command = new UpdateInterventionStatusCommand(
            Id: Guid.NewGuid(),
            Status: "completed",
            OutcomeNotes: "");

        var result = _interventionStatusCommandValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OutcomeNotes);
    }

    [Theory]
    [InlineData("planned")]
    [InlineData("in_progress")]
    [InlineData("completed")]
    [InlineData("cancelled")]
    public void InterventionStatusValidator_AllowedStatuses_ShouldPassValidation(string status)
    {
        var request = new UpdateInterventionStatusRequest(
            Status: status,
            OutcomeNotes: status == "completed" ? "Successfully completed remediation" : null);

        var result = _interventionStatusRequestValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("pending")]
    [InlineData("closed")]
    [InlineData("archived")]
    public void InterventionStatusValidator_DisallowedStatus_ShouldFailValidation(string invalidStatus)
    {
        var request = new UpdateInterventionStatusRequest(
            Status: invalidStatus,
            OutcomeNotes: null);

        var result = _interventionStatusRequestValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void InitiateInterventionValidator_ValidCommand_ShouldPassValidation()
    {
        var command = new InitiateAcademicInterventionCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            InterventionType: "remediation",
            TriggerType: "assessment_below_threshold",
            TriggerDescription: "Formative quiz score was 52% (below 70% threshold)",
            PlanDescription: "Individual tutoring twice weekly for two weeks");

        var result = _interventionInitiateValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("invalid_type")]
    [InlineData("")]
    public void InitiateInterventionValidator_InvalidType_ShouldFailValidation(string invalidType)
    {
        var command = new InitiateAcademicInterventionCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            InterventionType: invalidType,
            TriggerType: "missing_work",
            TriggerDescription: "Three assignments not turned in",
            PlanDescription: "Study hall supervision");

        var result = _interventionInitiateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InterventionType);
    }

    #endregion

    #region 6. Submodule 4 DTO Mappings & Invariants

    [Fact]
    public void StudentAcademicStandingDto_ShouldExposeStandingIdAliasAndProperties()
    {
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var determinedAt = DateTimeOffset.UtcNow;

        var dto = new StudentAcademicStandingDto(
            Id: id,
            StudentId: studentId,
            AcademicPeriodId: periodId,
            Standing: "conditional_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: "Must complete Math remediation",
            IsException: true,
            ExceptionReason: "Authorized decree",
            CommitteeReference: "SK Dewan Guru 01/2026",
            AuthorizedBy: "academic.authority",
            DeterminedAt: determinedAt);

        dto.Id.Should().Be(id);
        dto.StandingId.Should().Be(id);
        dto.StudentId.Should().Be(studentId);
        dto.Standing.Should().Be("conditional_standing");
        dto.IsException.Should().BeTrue();
        dto.CommitteeReference.Should().Be("SK Dewan Guru 01/2026");
    }

    [Fact]
    public void PromotionRecommendationDto_ShouldExposeRecommendationIdAliasAndProperties()
    {
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var standingId = Guid.NewGuid();
        var issuedAt = DateTimeOffset.UtcNow;

        var dto = new PromotionRecommendationDto(
            Id: id,
            StudentId: studentId,
            AcademicPeriodId: periodId,
            Recommendation: "promotion_eligible",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            Conditions: null,
            SupportingEvidence: "5 of 5 units completed; 92.5% attendance",
            AcademicStandingId: standingId,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null,
            AuthorizedBy: "academic.authority",
            IssuedAt: issuedAt);

        dto.Id.Should().Be(id);
        dto.RecommendationId.Should().Be(id);
        dto.StudentId.Should().Be(studentId);
        dto.Recommendation.Should().Be("promotion_eligible");
        dto.FromLevel.Should().Be("Grade 4");
        dto.RecommendedToLevel.Should().Be("Grade 5");
        dto.AcademicStandingId.Should().Be(standingId);
    }

    [Fact]
    public void AcademicAchievementDto_ShouldExposeAchievementIdAliasAndProperties()
    {
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var date = new DateOnly(2026, 6, 15);

        var dto = new AcademicAchievementDto(
            Id: id,
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            AchievementType: "academic_honors",
            Title: "Juara Umum Grade 4",
            Description: "Top overall academic performance",
            CriteriaMet: "GPA >= 3.8 and exemplary conduct",
            SupportingEvidence: "Report card semester 2",
            RecognizedBy: "academic.authority",
            RecognizedAt: date);

        dto.Id.Should().Be(id);
        dto.AchievementId.Should().Be(id);
        dto.Title.Should().Be("Juara Umum Grade 4");
        dto.AchievementType.Should().Be("academic_honors");
        dto.RecognizedAt.Should().Be(date);
    }

    [Fact]
    public void LearningProgressSummaryDto_ShouldExposeCountAliasAndProperties()
    {
        var dto = new LearningProgressSummaryDto(
            Status: "completed",
            UnitCount: 8);

        dto.Status.Should().Be("completed");
        dto.UnitCount.Should().Be(8);
        dto.Count.Should().Be(8);
    }

    #endregion

    #region Internal Test Request Records & FluentValidators

    public sealed record TestDetermineAcademicStandingRequest(
        Guid StudentId,
        Guid AcademicPeriodId,
        string Standing,
        string PolicyReference,
        string? Conditions,
        bool IsException,
        string? ExceptionReason = null,
        string? CommitteeReference = null);

    public sealed class TestDetermineAcademicStandingRequestValidator : AbstractValidator<TestDetermineAcademicStandingRequest>
    {
        private static readonly HashSet<string> _validStandings = new(StringComparer.OrdinalIgnoreCase)
        {
            "good_standing",
            "conditional_standing",
            "not_in_good_standing"
        };

        public TestDetermineAcademicStandingRequestValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("Student ID is required.");
            RuleFor(x => x.AcademicPeriodId).NotEmpty().WithMessage("Academic period ID is required.");

            RuleFor(x => x.Standing)
                .NotEmpty().WithMessage("Standing is required.")
                .Must(s => _validStandings.Contains(s))
                .WithMessage("Standing must be one of: good_standing, conditional_standing, not_in_good_standing.");

            RuleFor(x => x.PolicyReference)
                .NotEmpty().WithMessage("Policy reference is required.")
                .MaximumLength(100).WithMessage("Policy reference cannot exceed 100 characters.");

            When(x => string.Equals(x.Standing, "conditional_standing", StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Conditions)
                    .NotEmpty().WithMessage("Conditions are mandatory when standing is 'conditional_standing'.")
                    .MaximumLength(2000).WithMessage("Conditions cannot exceed 2000 characters.");
            });

            When(x => x.IsException, () =>
            {
                RuleFor(x => x.ExceptionReason)
                    .NotEmpty().WithMessage("Exception reason is required when is_exception is true.")
                    .MinimumLength(5).WithMessage("Exception reason must be at least 5 characters.")
                    .MaximumLength(1000).WithMessage("Exception reason cannot exceed 1000 characters.");

                RuleFor(x => x.CommitteeReference)
                    .NotEmpty().WithMessage("Committee reference is required when is_exception is true.")
                    .MinimumLength(5).WithMessage("Committee reference must be at least 5 characters.")
                    .MaximumLength(100).WithMessage("Committee reference cannot exceed 100 characters.");
            });
        }
    }

    public sealed record TestIssuePromotionRecommendationRequest(
        Guid StudentId,
        Guid AcademicPeriodId,
        string Recommendation,
        string FromLevel,
        string? RecommendedToLevel,
        string? Conditions,
        string SupportingEvidence,
        bool IsException,
        string? ExceptionReason = null,
        string? CommitteeReference = null);

    public sealed class TestIssuePromotionRecommendationRequestValidator : AbstractValidator<TestIssuePromotionRecommendationRequest>
    {
        private static readonly HashSet<string> _validRecommendations = new(StringComparer.OrdinalIgnoreCase)
        {
            "promotion_eligible",
            "conditional_promotion",
            "retention_recommended"
        };

        public TestIssuePromotionRecommendationRequestValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("Student ID is required.");
            RuleFor(x => x.AcademicPeriodId).NotEmpty().WithMessage("Academic period ID is required.");

            RuleFor(x => x.Recommendation)
                .NotEmpty().WithMessage("Recommendation is required.")
                .Must(r => _validRecommendations.Contains(r))
                .WithMessage("Recommendation must be one of: promotion_eligible, conditional_promotion, retention_recommended.");

            RuleFor(x => x.FromLevel)
                .NotEmpty().WithMessage("Current grade level (FromLevel) is required.")
                .MaximumLength(50).WithMessage("FromLevel cannot exceed 50 characters.");

            RuleFor(x => x.SupportingEvidence)
                .NotEmpty().WithMessage("Supporting evidence is required.")
                .MinimumLength(5).WithMessage("Supporting evidence must be at least 5 characters.")
                .MaximumLength(2000).WithMessage("Supporting evidence cannot exceed 2000 characters.");

            When(x => string.Equals(x.Recommendation, "conditional_promotion", StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Conditions)
                    .NotEmpty().WithMessage("Conditions are mandatory when recommendation is 'conditional_promotion'.")
                    .MaximumLength(2000).WithMessage("Conditions cannot exceed 2000 characters.");
            });

            When(x => x.IsException, () =>
            {
                RuleFor(x => x.ExceptionReason)
                    .NotEmpty().WithMessage("Exception reason is required when is_exception is true.")
                    .MinimumLength(5).WithMessage("Exception reason must be at least 5 characters.")
                    .MaximumLength(1000).WithMessage("Exception reason cannot exceed 1000 characters.");

                RuleFor(x => x.CommitteeReference)
                    .NotEmpty().WithMessage("Committee reference is required when is_exception is true.")
                    .MinimumLength(5).WithMessage("Committee reference must be at least 5 characters.")
                    .MaximumLength(100).WithMessage("Committee reference cannot exceed 100 characters.");
            });
        }
    }

    public sealed record TestCorrectAcademicRecordRequest(
        Guid RecordId,
        string NewValue,
        string Reason);

    public sealed class TestCorrectAcademicRecordRequestValidator : AbstractValidator<TestCorrectAcademicRecordRequest>
    {
        public TestCorrectAcademicRecordRequestValidator()
        {
            RuleFor(x => x.RecordId).NotEmpty().WithMessage("Record ID is required.");
            RuleFor(x => x.NewValue).NotEmpty().WithMessage("New value is required.");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Correction reason is required.")
                .MinimumLength(10).WithMessage("Correction reason must be at least 10 characters.")
                .MaximumLength(2000).WithMessage("Correction reason cannot exceed 2000 characters.");
        }
    }

    #endregion
}
