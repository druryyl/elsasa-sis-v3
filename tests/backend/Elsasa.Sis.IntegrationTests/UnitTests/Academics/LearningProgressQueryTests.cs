namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Data.Common;
using Elsasa.Sis.Academics.Features.LearningProgress.GetStudentLearningProgress;
using Elsasa.Sis.Academics.Features.LearningProgress.GetUnitLearningProgress;
using Elsasa.Sis.Shared.Database;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Learning Progress query DTOs and handler input validation (P2-S05, UC-ACD-12, SCR-ACD-07).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class LearningProgressQueryTests
{
    private sealed class DummySqlConnectionFactory : ISqlConnectionFactory
    {
        public DbConnection CreateConnection() => throw new NotImplementedException();
        public ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private readonly ISqlConnectionFactory _connectionFactory = new DummySqlConnectionFactory();

    [Fact]
    public void StudentLearningProgressItemDto_ShouldConstructWithAllProperties()
    {
        // Arrange
        var progressId = Guid.NewGuid();
        var participationId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var completionDate = new DateOnly(2026, 11, 7);
        var now = DateTimeOffset.UtcNow;

        // Act
        var dto = new StudentLearningProgressItemDto(
            ProgressId: progressId,
            ParticipationId: participationId,
            StudentId: studentId,
            LearningUnitId: unitId,
            LearningUnitCode: "MATH-04",
            LearningUnitName: "Elementary Mathematics 4",
            CreditWeight: 3.0m,
            AcademicPeriodId: periodId,
            SectionLabel: "4A",
            ParticipationStatus: "completed",
            ProgressStatus: "completed",
            Basis: "assessment_evidence",
            CompletionDate: completionDate,
            Notes: "PACE unit mastered",
            CreatedAt: now,
            CreatedBy: "teacher.rina",
            UpdatedAt: now,
            UpdatedBy: "teacher.rina");

        // Assert
        dto.ProgressId.Should().Be(progressId);
        dto.ParticipationId.Should().Be(participationId);
        dto.StudentId.Should().Be(studentId);
        dto.LearningUnitId.Should().Be(unitId);
        dto.LearningUnitCode.Should().Be("MATH-04");
        dto.LearningUnitName.Should().Be("Elementary Mathematics 4");
        dto.CreditWeight.Should().Be(3.0m);
        dto.AcademicPeriodId.Should().Be(periodId);
        dto.SectionLabel.Should().Be("4A");
        dto.ParticipationStatus.Should().Be("completed");
        dto.ProgressStatus.Should().Be("completed");
        dto.Basis.Should().Be("assessment_evidence");
        dto.CompletionDate.Should().Be(completionDate);
        dto.Notes.Should().Be("PACE unit mastered");
        dto.CreatedAt.Should().Be(now);
        dto.CreatedBy.Should().Be("teacher.rina");
    }

    [Fact]
    public void UnitLearningProgressItemDto_ShouldConstructWithAllProperties()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var participationId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var completionDate = new DateOnly(2026, 11, 7);
        var now = DateTimeOffset.UtcNow;

        // Act
        var dto = new UnitLearningProgressItemDto(
            StudentId: studentId,
            StudentNumber: "HAG-001",
            StudentName: "Rina Kusuma",
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            ParticipationId: participationId,
            ParticipationStatus: "in_progress",
            SectionLabel: "4A",
            ProgressId: progressId,
            ProgressStatus: "in_progress",
            Basis: "learning_work",
            OperativeGrade: "A",
            OperativeScore: 92.5m,
            GradeLetter: "A",
            GradeLabel: "Sangat Mahir",
            CompletionDate: completionDate,
            Notes: "Consistent performance",
            UpdatedAt: now);

        // Assert
        dto.StudentId.Should().Be(studentId);
        dto.StudentNumber.Should().Be("HAG-001");
        dto.StudentName.Should().Be("Rina Kusuma");
        dto.LearningUnitId.Should().Be(unitId);
        dto.AcademicPeriodId.Should().Be(periodId);
        dto.ParticipationId.Should().Be(participationId);
        dto.ParticipationStatus.Should().Be("in_progress");
        dto.SectionLabel.Should().Be("4A");
        dto.ProgressId.Should().Be(progressId);
        dto.ProgressStatus.Should().Be("in_progress");
        dto.Basis.Should().Be("learning_work");
        dto.OperativeGrade.Should().Be("A");
        dto.OperativeScore.Should().Be(92.5m);
        dto.GradeLetter.Should().Be("A");
        dto.GradeLabel.Should().Be("Sangat Mahir");
        dto.CompletionDate.Should().Be(completionDate);
        dto.Notes.Should().Be("Consistent performance");
        dto.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public async Task GetStudentLearningProgressHandler_EmptyStudentId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetStudentLearningProgressHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.StudentIdRequired");
    }

    [Fact]
    public async Task GetStudentLearningProgressHandler_EmptyPeriodId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetStudentLearningProgressHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.AcademicPeriodRequired");
    }

    [Fact]
    public async Task GetStudentLearningProgressHandler_NullPeriodId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetStudentLearningProgressHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.NewGuid(), null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.AcademicPeriodRequired");
    }

    [Fact]
    public async Task GetUnitLearningProgressHandler_EmptyUnitId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetUnitLearningProgressHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty, Guid.NewGuid(), null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.LearningUnitIdRequired");
    }

    [Fact]
    public async Task GetUnitLearningProgressHandler_EmptyPeriodId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetUnitLearningProgressHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.Empty, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.AcademicPeriodRequired");
    }

    [Fact]
    public async Task GetUnitLearningProgressHandler_NullPeriodId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetUnitLearningProgressHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.NewGuid(), null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.AcademicPeriodRequired");
    }
}
