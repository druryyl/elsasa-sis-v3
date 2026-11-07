namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.CohortReview;
using Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortProgression;
using Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ThresholdRuleEvaluator"/> and cohort review request validators (P3-S06, P6-S15, TD-03, UC-ACD-17).
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Unit test constant test fixtures")]
public sealed class ThresholdRuleEvaluatorTests
{
    private readonly ThresholdRuleEvaluator _evaluator = new();

    [Fact]
    public void EvaluateStanding_WhenAllUnitsPassedAndAttendanceSatisfied_ReturnsGoodStanding()
    {
        // Arrange
        var failedUnits = Array.Empty<string>();

        // Act
        var (standing, conditions) = _evaluator.EvaluateStanding(
            totalUnits: 5,
            completedUnits: 5,
            failedUnitNames: failedUnits,
            attendanceRate: 92.5m);

        // Assert
        standing.Should().Be(ThresholdRuleEvaluator.GoodStanding);
        conditions.Should().BeNull();
    }

    [Fact]
    public void EvaluateProgression_WhenGoodStanding_ReturnsPromotionEligibleToNextGrade()
    {
        // Act
        var (recommendation, nextLevel, conditions, evidence) = _evaluator.EvaluateProgression(
            standing: ThresholdRuleEvaluator.GoodStanding,
            fromLevel: "Grade 4",
            totalUnits: 5,
            completedUnits: 5,
            failedUnitNames: Array.Empty<string>(),
            attendanceRate: 92.5m);

        // Assert
        recommendation.Should().Be(ThresholdRuleEvaluator.PromotionEligible);
        nextLevel.Should().Be("Grade 5");
        conditions.Should().BeNull();
        evidence.Should().Contain("Good Standing").And.Contain("5/5 units completed");
    }

    [Fact]
    public void EvaluateStanding_WhenOneUnitFailed_ReturnsConditionalStandingWithGeneratedCondition()
    {
        // Arrange
        var failedUnits = new[] { "Mathematics" };

        // Act
        var (standing, conditions) = _evaluator.EvaluateStanding(
            totalUnits: 5,
            completedUnits: 4,
            failedUnitNames: failedUnits,
            attendanceRate: 85.0m);

        // Assert
        standing.Should().Be(ThresholdRuleEvaluator.ConditionalStanding);
        conditions.Should().Be("Must complete Mathematics remediation during holiday packet");
    }

    [Fact]
    public void EvaluateStanding_WhenTwoUnitsFailed_ReturnsConditionalStandingWithBothUnits()
    {
        // Arrange
        var failedUnits = new[] { "Mathematics", "Science" };

        // Act
        var (standing, conditions) = _evaluator.EvaluateStanding(
            totalUnits: 5,
            completedUnits: 3,
            failedUnitNames: failedUnits,
            attendanceRate: 80.0m);

        // Assert
        standing.Should().Be(ThresholdRuleEvaluator.ConditionalStanding);
        conditions.Should().Be("Must complete Mathematics and Science remediation during holiday packet");
    }

    [Fact]
    public void EvaluateProgression_WhenConditionalStanding_ReturnsConditionalPromotionWithAttachedConditions()
    {
        // Arrange
        var failedUnits = new[] { "Mathematics" };

        // Act
        var (recommendation, nextLevel, conditions, evidence) = _evaluator.EvaluateProgression(
            standing: ThresholdRuleEvaluator.ConditionalStanding,
            fromLevel: "Grade 4",
            totalUnits: 5,
            completedUnits: 4,
            failedUnitNames: failedUnits,
            attendanceRate: 85.0m,
            conditions: "Must complete Mathematics remediation during holiday packet");

        // Assert
        recommendation.Should().Be(ThresholdRuleEvaluator.ConditionalPromotion);
        nextLevel.Should().Be("Grade 5");
        conditions.Should().Be("Must complete Mathematics remediation during holiday packet");
        evidence.Should().Contain("Conditional Standing").And.Contain("Mathematics");
    }

    [Fact]
    public void EvaluateStanding_WhenMoreThanTwoUnitsFailed_ReturnsNotInGoodStanding()
    {
        // Arrange
        var failedUnits = new[] { "Mathematics", "Science", "English" };

        // Act
        var (standing, conditions) = _evaluator.EvaluateStanding(
            totalUnits: 5,
            completedUnits: 2,
            failedUnitNames: failedUnits,
            attendanceRate: 90.0m);

        // Assert
        standing.Should().Be(ThresholdRuleEvaluator.NotInGoodStanding);
        conditions.Should().BeNull();
    }

    [Fact]
    public void EvaluateStanding_WhenChronicNonAttendance_ReturnsNotInGoodStandingRegardlessOfUnits()
    {
        // Arrange: 0 units failed, but attendance is 68% (< 75%)
        var failedUnits = Array.Empty<string>();

        // Act
        var (standing, conditions) = _evaluator.EvaluateStanding(
            totalUnits: 5,
            completedUnits: 5,
            failedUnitNames: failedUnits,
            attendanceRate: 68.0m);

        // Assert
        standing.Should().Be(ThresholdRuleEvaluator.NotInGoodStanding);
        conditions.Should().BeNull();
    }

    [Fact]
    public void EvaluateProgression_WhenNotInGoodStanding_ReturnsRetentionRecommended()
    {
        // Arrange
        var failedUnits = new[] { "Mathematics", "Science", "English" };

        // Act
        var (recommendation, nextLevel, conditions, evidence) = _evaluator.EvaluateProgression(
            standing: ThresholdRuleEvaluator.NotInGoodStanding,
            fromLevel: "Grade 4",
            totalUnits: 5,
            completedUnits: 2,
            failedUnitNames: failedUnits,
            attendanceRate: 90.0m);

        // Assert
        recommendation.Should().Be(ThresholdRuleEvaluator.RetentionRecommended);
        nextLevel.Should().BeNull();
        conditions.Should().BeNull();
        evidence.Should().Contain("Not In Good Standing").And.Contain("3 unit(s) failed");
    }

    [Fact]
    public void EvaluateProgression_WhenChronicNonAttendance_ReturnsRetentionWithAttendanceEvidence()
    {
        // Act
        var (recommendation, nextLevel, conditions, evidence) = _evaluator.EvaluateProgression(
            standing: ThresholdRuleEvaluator.NotInGoodStanding,
            fromLevel: "Grade 4",
            totalUnits: 5,
            completedUnits: 5,
            failedUnitNames: Array.Empty<string>(),
            attendanceRate: 70.0m);

        // Assert
        recommendation.Should().Be(ThresholdRuleEvaluator.RetentionRecommended);
        nextLevel.Should().BeNull();
        evidence.Should().Contain("Chronic non-attendance").And.Contain("70.00% < 75%");
    }

    [Theory]
    [InlineData("Grade 1", "Grade 2")]
    [InlineData("Grade 4", "Grade 5")]
    [InlineData("Grade 8", "Grade 9")]
    [InlineData("Grade 10", "Grade 11")]
    [InlineData("Grade 11", "Grade 12")]
    [InlineData("Grade 12", "Graduated")]
    [InlineData("Kelas 4", "Kelas 5")]
    [InlineData("4", "5")]
    public void CalculateNextGradeLevel_ReturnsExpectedProgression(string fromLevel, string expectedNext)
    {
        var result = ThresholdRuleEvaluator.CalculateNextGradeLevel(fromLevel);
        result.Should().Be(expectedNext);
    }

    [Fact]
    public void CalculateNextGradeLevel_WhenNullOrEmpty_ReturnsNull()
    {
        ThresholdRuleEvaluator.CalculateNextGradeLevel(null).Should().BeNull();
        ThresholdRuleEvaluator.CalculateNextGradeLevel("   ").Should().BeNull();
    }

    [Fact]
    public void EvaluateCohortStandingValidator_ValidatesRequiredFields()
    {
        var validator = new EvaluateCohortStandingValidator();

        var invalidRequest = new EvaluateCohortStandingRequest(Guid.Empty, "");
        var result = validator.Validate(invalidRequest);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EvaluateCohortStandingRequest.AcademicPeriodId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EvaluateCohortStandingRequest.GradeLevel));

        var validRequest = new EvaluateCohortStandingRequest(Guid.NewGuid(), "Grade 4");
        validator.Validate(validRequest).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EvaluateCohortProgressionValidator_ValidatesRequiredFields()
    {
        var validator = new EvaluateCohortProgressionValidator();

        var invalidRequest = new EvaluateCohortProgressionRequest(Guid.Empty, "");
        var result = validator.Validate(invalidRequest);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EvaluateCohortProgressionRequest.AcademicPeriodId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EvaluateCohortProgressionRequest.GradeLevel));

        var validRequest = new EvaluateCohortProgressionRequest(Guid.NewGuid(), "Grade 4");
        validator.Validate(validRequest).IsValid.Should().BeTrue();
    }
    [Fact]
    public void EvaluateStanding_WhenAttendanceExactlyAt75PercentThreshold_Succeeds()
    {
        // Act: Attendance is exactly 75.0m (passing threshold)
        var (standing, conditions) = _evaluator.EvaluateStanding(
            totalUnits: 6,
            completedUnits: 6,
            failedUnitNames: Array.Empty<string>(),
            attendanceRate: 75.0m);

        // Assert
        standing.Should().Be(ThresholdRuleEvaluator.GoodStanding);
        conditions.Should().BeNull();
    }

    [Fact]
    public void EvaluateStanding_WhenAttendanceJustBelow75PercentThreshold_ReturnsNotInGoodStanding()
    {
        // Act: Attendance is 74.99m (< 75.0m)
        var (standing, conditions) = _evaluator.EvaluateStanding(
            totalUnits: 6,
            completedUnits: 6,
            failedUnitNames: Array.Empty<string>(),
            attendanceRate: 74.99m);

        // Assert
        standing.Should().Be(ThresholdRuleEvaluator.NotInGoodStanding);
        conditions.Should().BeNull();
    }

    [Fact]
    public void EvaluateStanding_WhenAttendanceBelow75PercentAndOneUnitFailed_ReturnsNotInGoodStanding()
    {
        // Arrange: 1 unit failed would normally be conditional standing, but attendance < 75% forces not_in_good_standing
        var (standing, conditions) = _evaluator.EvaluateStanding(
            totalUnits: 5,
            completedUnits: 4,
            failedUnitNames: new[] { "Mathematics" },
            attendanceRate: 72.0m);

        // Assert
        standing.Should().Be(ThresholdRuleEvaluator.NotInGoodStanding);
        conditions.Should().BeNull();
    }

    [Fact]
    public void EvaluateStanding_WhenFailedUnitNamesNull_ThrowsArgumentNullException()
    {
        var act = () => _evaluator.EvaluateStanding(5, 5, null!, 90.0m);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EvaluateProgression_WhenFailedUnitNamesNull_ThrowsArgumentNullException()
    {
        var act = () => _evaluator.EvaluateProgression(ThresholdRuleEvaluator.GoodStanding, "Grade 4", 5, 5, null!, 90.0m);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateConditionText_WithVariousUnitCounts_ReturnsExpectedFormat()
    {
        ThresholdRuleEvaluator.GenerateConditionText(null!).Should().BeNull();
        ThresholdRuleEvaluator.GenerateConditionText(Array.Empty<string>()).Should().BeNull();

        ThresholdRuleEvaluator.GenerateConditionText(new[] { "Mathematics" })
            .Should().Be("Must complete Mathematics remediation during holiday packet");

        ThresholdRuleEvaluator.GenerateConditionText(new[] { "Mathematics", "Science" })
            .Should().Be("Must complete Mathematics and Science remediation during holiday packet");

        ThresholdRuleEvaluator.GenerateConditionText(new[] { "Mathematics", "Science", "English" })
            .Should().Be("Must complete remediation for Mathematics, Science, English during holiday packet");
    }

    [Theory]
    [InlineData("Kindergarten", "Kindergarten")]
    [InlineData("Preparatory", "Preparatory")]
    [InlineData("Grade 12", "Graduated")]
    [InlineData("Grade 13", "Graduated")]
    [InlineData("Kelas 12", "Graduated")]
    public void CalculateNextGradeLevel_HandlesSpecialAndTerminalLevels(string fromLevel, string expectedNext)
    {
        var result = ThresholdRuleEvaluator.CalculateNextGradeLevel(fromLevel);
        result.Should().Be(expectedNext);
    }

    [Fact]
    public void CohortStandingProposalDto_InstantiatesWithExpectedProperties()
    {
        var studentId = Guid.NewGuid();
        var determinedAt = DateTimeOffset.UtcNow;
        var proposal = new CohortStandingProposalDto(
            StudentId: studentId,
            StudentNumber: "STU-001",
            StudentName: "Rina Kusuma",
            GradeLevel: "Grade 4",
            TotalUnits: 6,
            CompletedUnits: 5,
            FailedUnits: 1,
            AttendanceRate: 88.5m,
            ProposedStanding: ThresholdRuleEvaluator.ConditionalStanding,
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: "Must complete Math remediation",
            ExistingStanding: null,
            ExistingDeterminedAt: null,
            IsException: true,
            ExceptionReason: "Authorized by principal decree",
            CommitteeReference: "SK-001",
            FailingUnitNames: new[] { "Mathematics" });

        proposal.StudentId.Should().Be(studentId);
        proposal.StudentNumber.Should().Be("STU-001");
        proposal.ProposedStanding.Should().Be(ThresholdRuleEvaluator.ConditionalStanding);
        proposal.IsException.Should().BeTrue();
        proposal.CommitteeReference.Should().Be("SK-001");
        proposal.FailingUnitNames.Should().ContainSingle().Which.Should().Be("Mathematics");
    }

    [Fact]
    public void CohortProgressionProposalDto_InstantiatesWithExpectedProperties()
    {
        var studentId = Guid.NewGuid();
        var proposal = new CohortProgressionProposalDto(
            StudentId: studentId,
            StudentNumber: "STU-002",
            StudentName: "Budi Santoso",
            FromLevel: "Grade 4",
            RecommendedToLevel: "Grade 5",
            ProposedRecommendation: ThresholdRuleEvaluator.PromotionEligible,
            Conditions: null,
            SupportingEvidence: "All units passed",
            CurrentStanding: ThresholdRuleEvaluator.GoodStanding,
            IsStandingCommitted: true);

        proposal.StudentId.Should().Be(studentId);
        proposal.FromLevel.Should().Be("Grade 4");
        proposal.RecommendedToLevel.Should().Be("Grade 5");
        proposal.ProposedRecommendation.Should().Be(ThresholdRuleEvaluator.PromotionEligible);
        proposal.IsStandingCommitted.Should().BeTrue();
    }
}
