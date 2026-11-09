namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Data.Common;
using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Academics.Features.AcademicResults.GetStudentResultsWorkspace;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Academic Results Queries &amp; Student Portal Workspace (P4-S10, SCR-ACD-02, SCR-SELF-03).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class AcademicResultsQueriesTests
{
    private sealed class DummySqlConnectionFactory : ISqlConnectionFactory
    {
        public DbConnection CreateConnection() => throw new NotImplementedException();
        public ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    [Fact]
    public void AcademicResultDetailDto_Should_ConstructWithAllProperties_ForStudent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var date = new DateOnly(2026, 11, 20);

        // Act
        var dto = new AcademicResultDetailDto(
            Id: id,
            AssessmentId: assessmentId,
            StudentId: studentId,
            ApplicantId: null,
            StudentNumber: "STD-2026-001",
            CandidateName: "John Doe",
            AttemptNumber: 1,
            ReplacesResultId: null,
            IsFinal: true,
            ScoreNumeric: 95.5m,
            ScoreMax: 100m,
            GradeLetter: "A",
            GradeLabel: "Sangat Mahir",
            NarrativeFeedback: "Exemplary performance across all criteria.",
            EvaluationModel: "numeric",
            EvaluatedBy: "teacher.jane",
            EvaluatedDate: date,
            ResultStatus: "published",
            ApprovedBy: "authority.drury",
            ApprovedDate: date,
            PublishedDate: date,
            Notes: "Formative assignment review",
            CreatedAt: now,
            CreatedBy: "teacher.jane",
            UpdatedAt: now,
            UpdatedBy: "authority.drury");

        // Assert
        dto.Id.Should().Be(id);
        dto.AssessmentId.Should().Be(assessmentId);
        dto.StudentId.Should().Be(studentId);
        dto.ApplicantId.Should().BeNull();
        dto.StudentNumber.Should().Be("STD-2026-001");
        dto.CandidateName.Should().Be("John Doe");
        dto.AttemptNumber.Should().Be(1);
        dto.ReplacesResultId.Should().BeNull();
        dto.IsFinal.Should().BeTrue();
        dto.ScoreNumeric.Should().Be(95.5m);
        dto.ScoreMax.Should().Be(100m);
        dto.GradeLetter.Should().Be("A");
        dto.GradeLabel.Should().Be("Sangat Mahir");
        dto.NarrativeFeedback.Should().Be("Exemplary performance across all criteria.");
        dto.EvaluationModel.Should().Be("numeric");
        dto.EvaluatedBy.Should().Be("teacher.jane");
        dto.EvaluatedDate.Should().Be(date);
        dto.ResultStatus.Should().Be("published");
        dto.ApprovedBy.Should().Be("authority.drury");
        dto.ApprovedDate.Should().Be(date);
        dto.PublishedDate.Should().Be(date);
        dto.Notes.Should().Be("Formative assignment review");
    }

    [Fact]
    public void AcademicResultDetailDto_Should_ConstructWithAllProperties_ForApplicant()
    {
        // Arrange
        var id = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var date = new DateOnly(2026, 11, 20);

        // Act
        var dto = new AcademicResultDetailDto(
            Id: id,
            AssessmentId: assessmentId,
            StudentId: null,
            ApplicantId: applicantId,
            StudentNumber: null,
            CandidateName: "Sarah Connor",
            AttemptNumber: 2,
            ReplacesResultId: Guid.NewGuid(),
            IsFinal: true,
            ScoreNumeric: 82.0m,
            ScoreMax: 100m,
            GradeLetter: "B",
            GradeLabel: "Kompeten",
            NarrativeFeedback: "Entrance diagnostic retest passed.",
            EvaluationModel: "numeric",
            EvaluatedBy: "admission.officer",
            EvaluatedDate: date,
            ResultStatus: "approved",
            ApprovedBy: "coordinator.jane",
            ApprovedDate: date,
            PublishedDate: null,
            Notes: "Retake attempt after diagnostic gap",
            CreatedAt: now,
            CreatedBy: "admission.officer",
            UpdatedAt: now,
            UpdatedBy: "coordinator.jane");

        // Assert
        dto.Id.Should().Be(id);
        dto.AssessmentId.Should().Be(assessmentId);
        dto.StudentId.Should().BeNull();
        dto.ApplicantId.Should().Be(applicantId);
        dto.StudentNumber.Should().BeNull();
        dto.CandidateName.Should().Be("Sarah Connor");
        dto.AttemptNumber.Should().Be(2);
        dto.ReplacesResultId.Should().NotBeNull();
        dto.IsFinal.Should().BeTrue();
        dto.ScoreNumeric.Should().Be(82.0m);
        dto.GradeLetter.Should().Be("B");
        dto.ResultStatus.Should().Be("approved");
    }

    [Fact]
    public void StudentPublishedResultDto_Should_ConstructWithAllProperties()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var learningUnitId = Guid.NewGuid();
        var evalDate = new DateOnly(2026, 11, 15);
        var pubDate = new DateOnly(2026, 11, 16);

        // Act
        var dto = new StudentPublishedResultDto(
            ResultId: resultId,
            AssessmentId: assessmentId,
            AssessmentTitle: "Mid-Term Examination",
            AssessmentType: "summative",
            LearningUnitId: learningUnitId,
            LearningUnitCode: "MATH-10",
            LearningUnitTitle: "Mathematics Grade 10",
            AttemptNumber: 1,
            ScoreNumeric: 91.0m,
            ScoreMax: 100m,
            GradeLetter: "A",
            GradeLabel: "Sangat Mahir",
            NarrativeFeedback: "Well done on trigonometric proofs.",
            EvaluationModel: "numeric",
            EvaluatedDate: evalDate,
            PublishedDate: pubDate);

        // Assert
        dto.ResultId.Should().Be(resultId);
        dto.AssessmentId.Should().Be(assessmentId);
        dto.AssessmentTitle.Should().Be("Mid-Term Examination");
        dto.AssessmentType.Should().Be("summative");
        dto.LearningUnitId.Should().Be(learningUnitId);
        dto.LearningUnitCode.Should().Be("MATH-10");
        dto.LearningUnitTitle.Should().Be("Mathematics Grade 10");
        dto.AttemptNumber.Should().Be(1);
        dto.ScoreNumeric.Should().Be(91.0m);
        dto.ScoreMax.Should().Be(100m);
        dto.GradeLetter.Should().Be("A");
        dto.GradeLabel.Should().Be("Sangat Mahir");
        dto.NarrativeFeedback.Should().Be("Well done on trigonometric proofs.");
        dto.EvaluationModel.Should().Be("numeric");
        dto.EvaluatedDate.Should().Be(evalDate);
        dto.PublishedDate.Should().Be(pubDate);
    }

    [Fact]
    public async Task GetStudentResultsWorkspace_Should_Fail_When_AcademicPeriodId_IsEmpty()
    {
        // Arrange
        var connectionFactory = new DummySqlConnectionFactory();
        var currentUser = new TestCurrentUser
        {
            StudentId = Guid.NewGuid(),
            Roles = ["Student"]
        };

        var handler = new GetStudentResultsWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync(academicPeriodId: Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Academic.AcademicPeriodIdRequired");
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetStudentResultsWorkspace_Should_Fail_When_Student_Attempts_Accessing_Another_Student_Workspace()
    {
        // Arrange
        var connectionFactory = new DummySqlConnectionFactory();
        var myStudentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();

        var currentUser = new TestCurrentUser
        {
            StudentId = myStudentId,
            Roles = ["Student"]
        };

        var handler = new GetStudentResultsWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync(
            academicPeriodId: Guid.NewGuid(),
            studentIdOverride: otherStudentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Academic.UnauthorizedStudentWorkspace");
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task GetStudentResultsWorkspace_Should_Fail_When_User_Has_No_StudentId_And_Not_Privileged()
    {
        // Arrange
        var connectionFactory = new DummySqlConnectionFactory();
        var currentUser = new TestCurrentUser
        {
            StudentId = null,
            Roles = ["Guest"]
        };

        var handler = new GetStudentResultsWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync(academicPeriodId: Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Academic.UnauthorizedStudentWorkspace");
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task GetStudentResultsWorkspace_Should_Fail_When_Teacher_Does_Not_Provide_StudentId()
    {
        // Arrange
        var connectionFactory = new DummySqlConnectionFactory();
        var currentUser = new TestCurrentUser
        {
            StudentId = null,
            Roles = ["Teacher"]
        };

        var handler = new GetStudentResultsWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync(
            academicPeriodId: Guid.NewGuid(),
            studentIdOverride: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Academic.StudentIdRequired");
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetStudentResultsWorkspace_Should_Fail_When_Parent_Does_Not_Provide_StudentId()
    {
        // Arrange
        var connectionFactory = new DummySqlConnectionFactory();
        var currentUser = new TestCurrentUser
        {
            StudentId = null,
            Roles = ["Parent"]
        };

        var handler = new GetStudentResultsWorkspaceHandler(connectionFactory, currentUser);

        // Act
        var result = await handler.HandleAsync(
            academicPeriodId: Guid.NewGuid(),
            studentIdOverride: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Academic.StudentIdRequired");
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }
}
