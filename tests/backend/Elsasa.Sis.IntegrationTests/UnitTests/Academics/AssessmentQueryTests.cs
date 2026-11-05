namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Academics.Features.Assessments.GetAssessmentById;
using Elsasa.Sis.Academics.Features.Assessments.GetAssessmentsByUnit;
using Elsasa.Sis.Shared.Common;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Assessment Inspection & Queries (P2-S05, UC-ACD-05, ACD-WF-07).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class AssessmentQueryTests
{
    [Fact]
    public void AssessmentSummaryDto_Should_ConstructWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var date = new DateOnly(2026, 11, 20);

        // Act
        var dto = new AssessmentSummaryDto(
            Id: id,
            LearningUnitId: unitId,
            LearningActivityId: activityId,
            LearningActivityTitle: "Chapter 1 Lecture",
            Title: "Unit Quiz 1",
            AssessmentType: "formative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 75m,
            ReassessmentPolicy: "highest_score",
            Criteria: "{\"rubric\": \"standard\"}",
            AssessmentDate: date,
            AssessorReference: "Teacher Jane",
            Status: "planned",
            Notes: "Quiz covering basic concepts",
            EvidenceCount: 15,
            ResultCount: 12,
            CreatedAt: now,
            CreatedBy: "teacher.jane",
            UpdatedAt: now,
            UpdatedBy: "teacher.jane");

        // Assert
        dto.Id.Should().Be(id);
        dto.LearningUnitId.Should().Be(unitId);
        dto.LearningActivityId.Should().Be(activityId);
        dto.LearningActivityTitle.Should().Be("Chapter 1 Lecture");
        dto.Title.Should().Be("Unit Quiz 1");
        dto.AssessmentType.Should().Be("formative");
        dto.AssessmentMethod.Should().Be("written_test");
        dto.MaxScore.Should().Be(100m);
        dto.PassingScore.Should().Be(75m);
        dto.ReassessmentPolicy.Should().Be("highest_score");
        dto.Criteria.Should().Be("{\"rubric\": \"standard\"}");
        dto.AssessmentDate.Should().Be(date);
        dto.AssessorReference.Should().Be("Teacher Jane");
        dto.Status.Should().Be("planned");
        dto.Notes.Should().Be("Quiz covering basic concepts");
        dto.EvidenceCount.Should().Be(15);
        dto.ResultCount.Should().Be(12);
        dto.CreatedAt.Should().Be(now);
        dto.CreatedBy.Should().Be("teacher.jane");
        dto.UpdatedAt.Should().Be(now);
        dto.UpdatedBy.Should().Be("teacher.jane");
    }

    [Fact]
    public void AssessmentDetailDto_Should_ConstructWithParticipantCountsAndRubricSpecs()
    {
        // Arrange
        var id = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var date = new DateOnly(2026, 11, 20);

        // Act
        var dto = new AssessmentDetailDto(
            Id: id,
            LearningUnitId: unitId,
            LearningUnitTitle: "Mathematics Grade 10",
            LearningUnitCode: "MATH-10",
            LearningActivityId: activityId,
            LearningActivityTitle: "Mid-Term Preparation",
            Title: "Mid-Term Summative Assessment",
            AssessmentType: "summative",
            AssessmentMethod: "written_test",
            MaxScore: 100m,
            PassingScore: 75m,
            ReassessmentPolicy: "capped_at_passing",
            Criteria: "Detailed rubric specifications with 4 criteria bands",
            AssessmentDate: date,
            AssessorReference: "Teacher Jane",
            Status: "conducted",
            Notes: "Official exam hall session",
            EvidenceCount: 25,
            ResultCount: 24,
            ParticipantCount: 25,
            CreatedAt: now,
            CreatedBy: "admin",
            UpdatedAt: now,
            UpdatedBy: "teacher.jane");

        // Assert
        dto.Id.Should().Be(id);
        dto.LearningUnitId.Should().Be(unitId);
        dto.LearningUnitTitle.Should().Be("Mathematics Grade 10");
        dto.LearningUnitCode.Should().Be("MATH-10");
        dto.LearningActivityId.Should().Be(activityId);
        dto.LearningActivityTitle.Should().Be("Mid-Term Preparation");
        dto.Title.Should().Be("Mid-Term Summative Assessment");
        dto.AssessmentType.Should().Be("summative");
        dto.AssessmentMethod.Should().Be("written_test");
        dto.MaxScore.Should().Be(100m);
        dto.PassingScore.Should().Be(75m);
        dto.ReassessmentPolicy.Should().Be("capped_at_passing");
        dto.Criteria.Should().Be("Detailed rubric specifications with 4 criteria bands");
        dto.AssessmentDate.Should().Be(date);
        dto.AssessorReference.Should().Be("Teacher Jane");
        dto.Status.Should().Be("conducted");
        dto.Notes.Should().Be("Official exam hall session");
        dto.EvidenceCount.Should().Be(25);
        dto.ResultCount.Should().Be(24);
        dto.ParticipantCount.Should().Be(25);
        dto.CreatedAt.Should().Be(now);
        dto.CreatedBy.Should().Be("admin");
        dto.UpdatedAt.Should().Be(now);
        dto.UpdatedBy.Should().Be("teacher.jane");
    }

    [Fact]
    public void DomainErrors_Academics_Assessments_NotFound_ShouldHaveExpectedProperties()
    {
        // Act
        var error = DomainErrors.Academics.Assessments.NotFound;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("Academics.Assessments.NotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Permissions_Academics_Assessments_View_ShouldMatchExpectedConstant()
    {
        // Assert
        Permissions.Academics.Assessments.View.Should().Be("academics.assessments.view");
    }
}
