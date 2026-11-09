namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Services.Reassessment;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="IReassessmentPolicyEvaluator"/> and <see cref="ReassessmentPolicyEvaluator"/>
/// verifying retake outcome adjudication across all policies, tie-breaking, score capping,
/// and edge cases (GAP-004, TD-03, P3-S07, P7-S16).
/// </summary>
[Trait("Category", "UnitTest")]
[Trait("Category", "Unit")]
public sealed class ReassessmentPolicyEvaluatorTests
{
    private readonly ReassessmentPolicyEvaluator _evaluator = new();

    #region highest_score Policy Tests

    [Fact]
    public void HighestScore_Should_Select_Attempt_With_Maximum_Numeric_Score()
    {
        // Arrange
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 65m, "C", "Cukup", DateTime.UtcNow.AddDays(-10));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 88m, "B+", "Mahir", DateTime.UtcNow.AddDays(-5));
        var attempt3 = new AttemptScoreRecord(Guid.NewGuid(), 3, 75m, "B", "Kompeten", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: 75m,
            existingAttempts: [attempt1, attempt2],
            newAttempt: attempt3);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(88m);
        result.OperativeGradeLetter.Should().Be("B+");
        result.OperativeGradeLabel.Should().Be("Mahir");
        result.NonOperativeResultIds.Should().BeEquivalentTo([attempt1.Id, attempt3.Id]);
    }

    [Fact]
    public void HighestScore_Should_Break_Ties_By_Latest_Attempt_Number()
    {
        // Arrange: both attempts have identical score of 85
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 85m, "B+", "Kompeten", DateTime.UtcNow.AddDays(-7));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 85m, "B+", "Kompeten", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: 70m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert: later attempt (attempt 2) wins tie
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(85m);
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt1.Id);
    }

    [Fact]
    public void HighestScore_Should_Break_Ties_By_Evaluated_Date_When_Attempt_Numbers_Are_Equal()
    {
        // Arrange: same attempt number, different evaluation timestamps
        var earlierDate = DateTime.UtcNow.AddDays(-2);
        var laterDate = DateTime.UtcNow;
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 90m, "A", "Sangat Mahir", earlierDate);
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 1, 90m, "A", "Sangat Mahir", laterDate);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: 75m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt1.Id);
    }

    [Fact]
    public void HighestScore_Should_Fallback_To_Latest_Attempt_When_All_Scores_Are_Null()
    {
        // Arrange: Non-numeric evaluation models
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, null, "B", "Kompeten", DateTime.UtcNow.AddDays(-5));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, null, "A", "Sangat Mahir", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: null,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert: falls back to latest attempt
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().BeNull();
        result.OperativeGradeLetter.Should().Be("A");
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt1.Id);
    }

    #endregion

    #region capped_at_passing Policy Tests

    [Fact]
    public void CappedAtPassing_Should_Cap_Operative_Score_At_Passing_Score_When_Exceeded()
    {
        // Arrange: passing score is 75 (KKM), retake achieved 92
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 55m, "D", "Perlu Bimbingan", DateTime.UtcNow.AddDays(-5));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 92m, "A", "Sangat Mahir", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.CappedAtPassing,
            passingScore: 75m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert: attempt 2 is operative, but score capped at 75m
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(75m);
        result.OperativeGradeLetter.Should().Be("A");
        result.OperativeGradeLabel.Should().Be("Sangat Mahir");
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt1.Id);
    }

    [Fact]
    public void CappedAtPassing_Should_Preserve_Score_When_Below_Or_Equal_To_Passing_Score()
    {
        // Arrange: passing score 75, attempt 1 scored 50, attempt 2 scored 70 (still <= 75)
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 50m, "D", "Perlu Bimbingan", DateTime.UtcNow.AddDays(-5));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 70m, "C+", "Cukup", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.CappedAtPassing,
            passingScore: 75m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert: attempt 2 is operative with unmodified score 70m
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(70m);
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt1.Id);
    }

    [Fact]
    public void CappedAtPassing_Should_Preserve_Score_Exactly_When_Equal_To_Passing_Score()
    {
        // Arrange: passing score 75, attempt 2 scored exactly 75
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 60m, "C", "Cukup", DateTime.UtcNow.AddDays(-3));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 75m, "B", "Kompeten", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.CappedAtPassing,
            passingScore: 75m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(75m);
    }

    [Fact]
    public void CappedAtPassing_Should_Not_Cap_When_Passing_Score_Is_Null()
    {
        // Arrange: passing score is null, attempt 2 scored 90
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 70m, "B", "Kompeten", DateTime.UtcNow.AddDays(-3));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 90m, "A", "Sangat Mahir", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.CappedAtPassing,
            passingScore: null,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert: unconstrained score preserved
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(90m);
    }

    [Fact]
    public void CappedAtPassing_Should_Fallback_To_Latest_Attempt_When_All_Scores_Are_Null()
    {
        // Arrange
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, null, "C", null, DateTime.UtcNow.AddDays(-2));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, null, "B", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.CappedAtPassing,
            passingScore: 75m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().BeNull();
        result.OperativeGradeLetter.Should().Be("B");
    }

    #endregion

    #region latest_attempt Policy Tests

    [Fact]
    public void LatestAttempt_Should_Select_Latest_Attempt_Even_If_Score_Is_Lower()
    {
        // Arrange: attempt 1 scored 85, attempt 2 scored 65
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 85m, "B+", "Mahir", DateTime.UtcNow.AddDays(-7));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 65m, "C", "Cukup", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.LatestAttempt,
            passingScore: 70m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert: attempt 2 is operative despite lower score
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(65m);
        result.OperativeGradeLetter.Should().Be("C");
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt1.Id);
    }

    [Fact]
    public void LatestAttempt_Should_Break_Ties_By_Evaluated_Date()
    {
        // Arrange
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 2, 70m, "B", null, DateTime.UtcNow.AddHours(-1));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 80m, "A", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.LatestAttempt,
            passingScore: null,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(80m);
    }

    #endregion

    #region average Policy Tests

    [Fact]
    public void Average_Should_Compute_Mean_Across_All_Attempts_And_Assign_To_New_Attempt()
    {
        // Arrange: attempts 60, 80, 100 -> mean = 80.00
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 60m, "C", "Cukup", DateTime.UtcNow.AddDays(-10));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 80m, "B+", "Mahir", DateTime.UtcNow.AddDays(-5));
        var attempt3 = new AttemptScoreRecord(Guid.NewGuid(), 3, 100m, "A", "Sempurna", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.Average,
            passingScore: 75m,
            existingAttempts: [attempt1, attempt2],
            newAttempt: attempt3);

        // Assert
        result.OperativeResultId.Should().Be(attempt3.Id);
        result.OperativeScoreNumeric.Should().Be(80m);
        result.OperativeGradeLetter.Should().Be("A");
        result.OperativeGradeLabel.Should().Be("Sempurna");
        result.NonOperativeResultIds.Should().BeEquivalentTo([attempt1.Id, attempt2.Id]);
    }

    [Fact]
    public void Average_Should_Round_To_Two_Decimal_Places_Away_From_Zero()
    {
        // Arrange: 70 + 75 + 86 = 231 / 3 = 77.00
        // Another example: 75.50 + 80.00 + 85.00 = 240.50 / 3 = 80.1666... -> 80.17
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 75.50m, "B", null, DateTime.UtcNow.AddDays(-5));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 80.00m, "B+", null, DateTime.UtcNow.AddDays(-2));
        var attempt3 = new AttemptScoreRecord(Guid.NewGuid(), 3, 85.00m, "A", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.Average,
            passingScore: null,
            existingAttempts: [attempt1, attempt2],
            newAttempt: attempt3);

        // Assert
        result.OperativeResultId.Should().Be(attempt3.Id);
        result.OperativeScoreNumeric.Should().Be(80.17m);
    }

    [Fact]
    public void Average_Should_Preserve_New_Attempt_Score_When_No_Scored_Attempts_Exist()
    {
        // Arrange: Non-numeric evaluation models
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, null, "Pass", null, DateTime.UtcNow.AddDays(-3));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, null, "Pass", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.Average,
            passingScore: null,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().BeNull();
        result.OperativeGradeLetter.Should().Be("Pass");
    }

    #endregion

    #region none Policy Tests

    [Fact]
    public void None_Should_Always_Keep_Attempt_One_As_Operative()
    {
        // Arrange: attempt 1 scored 50, attempt 2 scored 95
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 50m, "D", "Perlu Bimbingan", DateTime.UtcNow.AddDays(-7));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 95m, "A", "Sangat Mahir", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.None,
            passingScore: 75m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert: attempt 1 remains operative, attempt 2 is non-operative
        result.OperativeResultId.Should().Be(attempt1.Id);
        result.OperativeScoreNumeric.Should().Be(50m);
        result.OperativeGradeLetter.Should().Be("D");
        result.OperativeGradeLabel.Should().Be("Perlu Bimbingan");
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt2.Id);
    }

    [Fact]
    public void None_Should_Select_Lowest_Attempt_Number_When_Attempt_One_Is_Not_Explicitly_Numbered_One()
    {
        // Arrange: attempt sequence starting from 2
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 60m, "C", null, DateTime.UtcNow.AddDays(-3));
        var attempt3 = new AttemptScoreRecord(Guid.NewGuid(), 3, 85m, "B+", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.None,
            passingScore: null,
            existingAttempts: [attempt2],
            newAttempt: attempt3);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(60m);
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt3.Id);
    }

    #endregion

    #region Edge Cases & Resiliency Tests

    [Fact]
    public void Evaluate_Should_Handle_Single_Attempt_With_Zero_Score()
    {
        // Arrange
        var attempt = new AttemptScoreRecord(Guid.NewGuid(), 1, 0m, "F", "Nol", DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: 70m,
            existingAttempts: [],
            newAttempt: attempt);

        // Assert
        result.OperativeResultId.Should().Be(attempt.Id);
        result.OperativeScoreNumeric.Should().Be(0m);
        result.OperativeGradeLetter.Should().Be("F");
        result.NonOperativeResultIds.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_Should_Correctly_Distinguish_Zero_From_Higher_Score()
    {
        // Arrange: attempt 1 scored 0, attempt 2 scored 50
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 0m, "F", null, DateTime.UtcNow.AddDays(-5));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 50m, "D", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: 70m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(50m);
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt1.Id);
    }

    [Fact]
    public void Evaluate_Should_Handle_Null_Existing_Attempts_Gracefully()
    {
        // Arrange
        var attempt = new AttemptScoreRecord(Guid.NewGuid(), 1, 85m, "B+", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: 75m,
            existingAttempts: null!,
            newAttempt: attempt);

        // Assert
        result.OperativeResultId.Should().Be(attempt.Id);
        result.OperativeScoreNumeric.Should().Be(85m);
        result.NonOperativeResultIds.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_Should_Throw_ArgumentNullException_When_NewAttempt_Is_Null()
    {
        // Act
        var act = () => _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: 75m,
            existingAttempts: [],
            newAttempt: null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("HIGHEST_SCORE")]
    [InlineData(" highest_score ")]
    [InlineData("Highest_Score")]
    public void Evaluate_Should_Be_Case_And_Whitespace_Insensitive(string policyVariant)
    {
        // Arrange
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 70m, "B", null, DateTime.UtcNow.AddDays(-2));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 90m, "A", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            policyVariant,
            passingScore: 75m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(90m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("non_existent_policy")]
    public void Evaluate_Should_Default_To_Latest_Attempt_When_Policy_Is_Unrecognized(string? policy)
    {
        // Arrange: attempt 1 scored 90, attempt 2 scored 60
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 90m, "A", null, DateTime.UtcNow.AddDays(-3));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 60m, "C", null, DateTime.UtcNow);

        // Act
        var result = _evaluator.Evaluate(
            policy!,
            passingScore: 75m,
            existingAttempts: [attempt1],
            newAttempt: attempt2);

        // Assert: latest attempt (attempt 2) wins
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.OperativeScoreNumeric.Should().Be(60m);
    }

    [Fact]
    public void Evaluate_Should_Not_Duplicate_When_NewAttempt_Is_Already_In_ExistingAttempts()
    {
        // Arrange
        var attempt1 = new AttemptScoreRecord(Guid.NewGuid(), 1, 70m, "B", null, DateTime.UtcNow.AddDays(-2));
        var attempt2 = new AttemptScoreRecord(Guid.NewGuid(), 2, 90m, "A", null, DateTime.UtcNow);

        // Act: passing attempt2 in both existingAttempts and newAttempt
        var result = _evaluator.Evaluate(
            ReassessmentPolicies.HighestScore,
            passingScore: 75m,
            existingAttempts: [attempt1, attempt2],
            newAttempt: attempt2);

        // Assert: attempt 2 is operative, only attempt 1 in non-operative
        result.OperativeResultId.Should().Be(attempt2.Id);
        result.NonOperativeResultIds.Should().ContainSingle().Which.Should().Be(attempt1.Id);
    }

    #endregion
}
