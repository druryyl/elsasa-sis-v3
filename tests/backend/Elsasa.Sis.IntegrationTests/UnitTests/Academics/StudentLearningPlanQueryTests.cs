namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetActiveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlanById;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Database;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Unit tests for Student Learning Plan query vertical slices, DTOs, and DI registrations (P2-S06, SCR-ACD-04).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class StudentLearningPlanQueryTests
{
    #region PagedResult Tests

    [Fact]
    public void PagedResult_Create_Should_CalculatePaginationMetadataCorrectly()
    {
        // Arrange
        var items = new List<string> { "Plan1", "Plan2", "Plan3" };
        var totalCount = 45;
        var pageNumber = 2;
        var pageSize = 10;

        // Act
        var result = PagedResult<string>.Create(items, totalCount, pageNumber, pageSize);

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(45);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(5);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_Create_Should_HandleFirstPage_WithNoPreviousPage()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };
        var totalCount = 25;
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = PagedResult<int>.Create(items, totalCount, pageNumber, pageSize);

        // Assert
        result.PageNumber.Should().Be(1);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_Create_Should_HandleLastPage_WithNoNextPage()
    {
        // Arrange
        var items = new List<int> { 21, 22 };
        var totalCount = 22;
        var pageNumber = 3;
        var pageSize = 10;

        // Act
        var result = PagedResult<int>.Create(items, totalCount, pageNumber, pageSize);

        // Assert
        result.PageNumber.Should().Be(3);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-5, -10, 1, 1)]
    public void PagedResult_Create_Should_EnforceSafeDefaults_WhenPageOrSizeIsInvalid(
        int invalidPage,
        int invalidPageSize,
        int expectedPage,
        int expectedSize)
    {
        // Arrange
        var items = new List<string>();

        // Act
        var result = PagedResult<string>.Create(items, 0, invalidPage, invalidPageSize);

        // Assert
        result.PageNumber.Should().Be(expectedPage);
        result.PageSize.Should().Be(expectedSize);
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    #endregion

    #region Query & DTO Tests

    [Fact]
    public void GetStudentLearningPlansQuery_Should_HaveExpectedDefaultValues()
    {
        // Act
        var query = new GetStudentLearningPlansQuery();

        // Assert
        query.AcademicPeriodId.Should().BeNull();
        query.Status.Should().BeNull();
        query.StudentSearch.Should().BeNull();
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
    }

    [Fact]
    public void StudentLearningPlanListItemDto_Should_RetainAllAssignedProperties()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var item = new StudentLearningPlanListItemDto(
            Id: planId,
            StudentId: studentId,
            StudentName: "Budi Santoso",
            StudentNis: "0071234567",
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Status: "active",
            Version: 2,
            UnitsCount: 3,
            AdjustmentsCount: 2,
            CreatedAt: now,
            UpdatedAt: now.AddDays(1));

        // Assert
        item.Id.Should().Be(planId);
        item.StudentId.Should().Be(studentId);
        item.StudentName.Should().Be("Budi Santoso");
        item.StudentNis.Should().Be("0071234567");
        item.AcademicPeriodId.Should().Be(periodId);
        item.AcademicPeriodLabel.Should().Be("Semester 1 TP 2029/2030");
        item.Status.Should().Be("active");
        item.Version.Should().Be(2);
        item.UnitsCount.Should().Be(3);
        item.AdjustmentsCount.Should().Be(2);
        item.CreatedAt.Should().Be(now);
        item.UpdatedAt.Should().Be(now.AddDays(1));
    }

    [Fact]
    public void StudentLearningPlanDetailDto_Should_RetainAllAssignedProperties_And_NestedCollections()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var units = new List<SlpLearningUnitDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "MAT-03-S1", "Remedial Math Grade 3", 1, true, "Grade 3 pacing"),
            new(Guid.NewGuid(), Guid.NewGuid(), "IND-04-S1", "Bahasa Indonesia 4", 2, true, null)
        };

        var adjustments = new List<AcademicPlanAdjustmentDto>
        {
            new(
                Guid.NewGuid(),
                "accommodation",
                "50% extra exam time",
                "Psychological recommendation",
                new DateOnly(2029, 9, 1),
                new DateOnly(2030, 6, 30),
                "ratna.vp",
                new DateOnly(2029, 9, 2),
                null)
        };

        var approvals = new List<SlpApprovalDto>
        {
            new(
                Guid.NewGuid(),
                1,
                "ratna.vp",
                new DateOnly(2029, 9, 2),
                "initial_approval",
                "Approved initial learning plan")
        };

        // Act
        var detail = new StudentLearningPlanDetailDto(
            Id: planId,
            StudentId: studentId,
            StudentName: "Budi Santoso",
            StudentNis: "0071234567",
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Status: "active",
            Version: 1,
            Notes: "Special education plan notes.",
            CreatedAt: now,
            CreatedBy: "coordinator.jane",
            UpdatedAt: null,
            UpdatedBy: null,
            ConstituentUnits: units,
            Adjustments: adjustments,
            Approvals: approvals);

        // Assert
        detail.Id.Should().Be(planId);
        detail.StudentId.Should().Be(studentId);
        detail.StudentName.Should().Be("Budi Santoso");
        detail.StudentNis.Should().Be("0071234567");
        detail.ConstituentUnits.Should().HaveCount(2);
        detail.ConstituentUnits[0].UnitCode.Should().Be("MAT-03-S1");
        detail.Adjustments.Should().HaveCount(1);
        detail.Adjustments[0].AdjustmentType.Should().Be("accommodation");
        detail.Approvals.Should().HaveCount(1);
        detail.Approvals[0].ApprovalType.Should().Be("initial_approval");
    }

    #endregion

    private sealed class DummySqlConnectionFactory : ISqlConnectionFactory
    {
        public ValueTask<System.Data.Common.DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    #region Handler Validation Tests

    [Fact]
    public async Task GetStudentLearningPlanByIdHandler_Should_Fail_When_IdIsEmpty()
    {
        // Arrange
        var dummyConnectionFactory = new DummySqlConnectionFactory();
        var handler = new GetStudentLearningPlanByIdHandler(dummyConnectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.InvalidPlanId");
    }

    [Fact]
    public async Task GetActiveStudentLearningPlanHandler_Should_Fail_When_StudentIdIsEmpty()
    {
        // Arrange
        var dummyConnectionFactory = new DummySqlConnectionFactory();
        var handler = new GetActiveStudentLearningPlanHandler(dummyConnectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.InvalidStudentId");
    }

    [Fact]
    public async Task GetActiveStudentLearningPlanHandler_Should_Fail_When_AcademicPeriodIdIsEmpty()
    {
        // Arrange
        var dummyConnectionFactory = new DummySqlConnectionFactory();
        var handler = new GetActiveStudentLearningPlanHandler(dummyConnectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.InvalidAcademicPeriodId");
    }

    #endregion

    #region DI Registration Tests

    [Fact]
    public void AddAcademicsDomain_Should_RegisterLearningPlansQueryHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAcademicsDomain();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(GetStudentLearningPlanByIdHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(GetStudentLearningPlansHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(GetActiveStudentLearningPlanHandler)
            && sd.Lifetime == ServiceLifetime.Scoped);
    }

    #endregion
}
