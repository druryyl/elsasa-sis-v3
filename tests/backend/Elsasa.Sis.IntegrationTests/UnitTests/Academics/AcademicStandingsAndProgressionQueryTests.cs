namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Data.Common;
using Elsasa.Sis.Academics;
using Elsasa.Sis.Academics.Features.AcademicStandings.GetCohortStandings;
using Elsasa.Sis.Academics.Features.AcademicStandings.GetStudentStandings;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.GetCohortPromotionRecommendations;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.GetStudentPromotionRecommendations;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Database;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Academic Standings and Progression query DTOs, handlers, and validation rules (P3-S09, UC-ACD-17, SCR-ACD-09).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class AcademicStandingsAndProgressionQueryTests
{
    private sealed class DummySqlConnectionFactory : ISqlConnectionFactory
    {
        public DbConnection CreateConnection() => throw new NotImplementedException();
        public ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private readonly ISqlConnectionFactory _connectionFactory = new DummySqlConnectionFactory();

    [Fact]
    public void CohortStandingItemDto_ShouldConstructWithAllPropertiesAndAlias()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var dto = new CohortStandingItemDto(
            Id: id,
            StudentId: studentId,
            StudentNumber: "HAG-001",
            StudentName: "Rina Kusuma",
            GradeLevel: "10",
            AcademicPeriodId: periodId,
            Standing: "good_standing",
            PolicyReference: "SK-AKAD/2026/POLICY-01",
            Conditions: null,
            IsException: true,
            ExceptionReason: "Granted committee exception due to academic achievements",
            CommitteeReference: "SK-DG/2026/11-04",
            AuthorizedBy: "academic.head",
            DeterminedAt: now,
            CreatedAt: now,
            UpdatedAt: now);

        // Assert
        dto.Id.Should().Be(id);
        dto.StandingId.Should().Be(id);
        dto.StudentId.Should().Be(studentId);
        dto.StudentNumber.Should().Be("HAG-001");
        dto.StudentName.Should().Be("Rina Kusuma");
        dto.GradeLevel.Should().Be("10");
        dto.AcademicPeriodId.Should().Be(periodId);
        dto.Standing.Should().Be("good_standing");
        dto.PolicyReference.Should().Be("SK-AKAD/2026/POLICY-01");
        dto.Conditions.Should().BeNull();
        dto.IsException.Should().BeTrue();
        dto.ExceptionReason.Should().Be("Granted committee exception due to academic achievements");
        dto.CommitteeReference.Should().Be("SK-DG/2026/11-04");
        dto.AuthorizedBy.Should().Be("academic.head");
        dto.DeterminedAt.Should().Be(now);
        dto.CreatedAt.Should().Be(now);
        dto.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void CohortPromotionRecommendationItemDto_ShouldConstructWithAllPropertiesAndAlias()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var standingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var dto = new CohortPromotionRecommendationItemDto(
            Id: id,
            StudentId: studentId,
            StudentNumber: "HAG-002",
            StudentName: "Budi Santoso",
            AcademicPeriodId: periodId,
            Recommendation: "conditional_promotion",
            FromLevel: "Grade 10",
            RecommendedToLevel: "Grade 11",
            Conditions: "Must complete Mathematics remediation during holiday packet.",
            SupportingEvidence: "Passed 7 of 8 units with 88.5% attendance.",
            AcademicStandingId: standingId,
            IsException: false,
            ExceptionReason: null,
            CommitteeReference: null,
            AuthorizedBy: "academic.head",
            IssuedAt: now,
            CreatedAt: now,
            UpdatedAt: now);

        // Assert
        dto.Id.Should().Be(id);
        dto.RecommendationId.Should().Be(id);
        dto.StudentId.Should().Be(studentId);
        dto.StudentNumber.Should().Be("HAG-002");
        dto.StudentName.Should().Be("Budi Santoso");
        dto.AcademicPeriodId.Should().Be(periodId);
        dto.Recommendation.Should().Be("conditional_promotion");
        dto.FromLevel.Should().Be("Grade 10");
        dto.RecommendedToLevel.Should().Be("Grade 11");
        dto.Conditions.Should().Be("Must complete Mathematics remediation during holiday packet.");
        dto.SupportingEvidence.Should().Be("Passed 7 of 8 units with 88.5% attendance.");
        dto.AcademicStandingId.Should().Be(standingId);
        dto.IsException.Should().BeFalse();
        dto.ExceptionReason.Should().BeNull();
        dto.CommitteeReference.Should().BeNull();
        dto.AuthorizedBy.Should().Be("academic.head");
        dto.IssuedAt.Should().Be(now);
        dto.CreatedAt.Should().Be(now);
        dto.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public async Task GetStudentStandingsHandler_EmptyStudentId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetStudentStandingsHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.StudentIdRequired");
    }

    [Fact]
    public async Task GetCohortStandingsHandler_EmptyPeriodId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetCohortStandingsHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.AcademicPeriodRequired");
    }

    [Fact]
    public async Task GetStudentPromotionRecommendationsHandler_EmptyStudentId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetStudentPromotionRecommendationsHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.StudentIdRequired");
    }

    [Fact]
    public async Task GetCohortPromotionRecommendationsHandler_EmptyPeriodId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetCohortPromotionRecommendationsHandler(_connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be("Academics.AcademicPeriodRequired");
    }

    [Fact]
    public void Permissions_ShouldDefineRequiredStandingAndProgressionViewPermissions()
    {
        // Assert
        Permissions.Academics.Standings.View.Should().Be("academics.standings.view");
        Permissions.Academics.Progression.View.Should().Be("academics.progression.view");
    }
}
