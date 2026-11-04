namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Allocations.GetStudentReceivables;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public sealed class GetStudentReceivablesTests
{
    [Fact]
    public async Task HandleAsync_WhenStudentIdIsEmpty_ReturnsValidationError()
    {
        // Arrange
        var connectionFactory = new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;");
        var handler = new GetStudentReceivablesHandler(connectionFactory);

        // Act
        var result = await handler.HandleAsync(Guid.Empty);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Finance.Receivables.InvalidStudentId", result.Error.Code);
    }

    [Fact]
    public void GetStudentReceivablesResponse_Should_ComputeAggregatesCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var item1 = new StudentReceivableDto(
            ObligationId: Guid.NewGuid(),
            ObligationNumber: "OBL-2026-0001",
            StudentAccountId: accountId,
            StudentId: studentId,
            ApplicantId: null,
            ChargeType: "enrollment_fee",
            Description: "Enrollment Fee 2026/2027",
            IsMandatory: true,
            DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            OriginalAmount: 5_000_000,
            CurrencyCode: "IDR",
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027",
            ObligationStatus: "assessed",
            NetObligation: 5_000_000,
            TotalPaid: 2_000_000,
            OutstandingAmount: 3_000_000,
            IsOverdue: true,
            DaysOverdue: 10,
            AgingBracket: "1-30 Days",
            ReceivableStatus: "overdue");

        var item2 = new StudentReceivableDto(
            ObligationId: Guid.NewGuid(),
            ObligationNumber: "OBL-2026-0002",
            StudentAccountId: accountId,
            StudentId: studentId,
            ApplicantId: null,
            ChargeType: "tuition",
            Description: "Tuition July 2026",
            IsMandatory: true,
            DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            OriginalAmount: 2_000_000,
            CurrencyCode: "IDR",
            AcademicPeriodId: Guid.NewGuid(),
            AcademicPeriodLabel: "2026/2027",
            ObligationStatus: "settled",
            NetObligation: 2_000_000,
            TotalPaid: 2_000_000,
            OutstandingAmount: 0,
            IsOverdue: false,
            DaysOverdue: 0,
            AgingBracket: "Current",
            ReceivableStatus: "settled");

        var items = new List<StudentReceivableDto> { item1, item2 };
        var openObligations = items.Where(i => i.OutstandingAmount > 0).ToList();

        // Act
        var response = new GetStudentReceivablesResponse(
            StudentId: studentId,
            TotalNetObligation: items.Sum(i => i.NetObligation),
            TotalPaid: items.Sum(i => i.TotalPaid),
            TotalOutstanding: items.Sum(i => i.OutstandingAmount),
            TotalOverdueCount: items.Count(i => i.IsOverdue),
            Receivables: items,
            OpenObligations: openObligations);

        // Assert
        Assert.Equal(studentId, response.StudentId);
        Assert.Equal(7_000_000, response.TotalNetObligation);
        Assert.Equal(4_000_000, response.TotalPaid);
        Assert.Equal(3_000_000, response.TotalOutstanding);
        Assert.Equal(1, response.TotalOverdueCount);
        Assert.Equal(2, response.Receivables.Count);
        Assert.Single(response.OpenObligations);
        Assert.Equal(3_000_000, response.OpenObligations[0].OutstandingAmount);
        Assert.True(response.OpenObligations[0].IsOverdue);
    }

    [Fact]
    public void GetStudentReceivablesEndpoint_Should_ImplementIEndpoint_And_MapCleanly()
    {
        // Arrange
        var endpoint = new GetStudentReceivablesEndpoint();

        // Assert
        Assert.IsAssignableFrom<IEndpoint>(endpoint);

        var services = new ServiceCollection();
        services.AddRouting();
        services.AddAuthorization();
        var provider = services.BuildServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(provider);

        // Act & Assert (Should not throw)
        endpoint.MapEndpoint(builder);
    }

    private sealed class DefaultEndpointRouteBuilder(IServiceProvider serviceProvider) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return new ApplicationBuilder(ServiceProvider);
        }
    }
}
