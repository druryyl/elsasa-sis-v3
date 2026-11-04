namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance;
using Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;
using Elsasa.Sis.Finance.Features.Billing.GenerateStatement;
using Elsasa.Sis.Finance.Features.Billing.GetBillingDocumentById;
using Elsasa.Sis.Finance.Features.Billing.GetBillingDocuments;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.DependencyInjection;

public sealed class BillingDocumentTests
{
    private readonly GenerateBillingDocumentValidator _billValidator = new();
    private readonly GenerateStatementValidator _statementValidator = new();

    [Fact]
    public void BillValidator_ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.NewGuid(),
            ObligationIds: [Guid.NewGuid(), Guid.NewGuid()],
            PaymentDueDate: new DateOnly(2026, 12, 15),
            Notes: "Consolidated November tuition bill");

        // Act
        var result = _billValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void BillValidator_EmptyStudentId_ShouldFailValidation()
    {
        // Arrange
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.Empty);

        // Act
        var result = _billValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.StudentId));
    }

    [Fact]
    public void BillValidator_NotesExceeding1000Chars_ShouldFailValidation()
    {
        // Arrange
        var request = new GenerateBillingDocumentRequest(
            StudentId: Guid.NewGuid(),
            Notes: new string('N', 1001));

        // Act
        var result = _billValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Notes));
    }

    [Fact]
    public void StatementValidator_ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new GenerateStatementRequest(
            StudentId: Guid.NewGuid(),
            BillingPeriodStart: new DateOnly(2026, 1, 1),
            BillingPeriodEnd: new DateOnly(2026, 6, 30),
            Notes: "Semester 1 Financial Statement");

        // Act
        var result = _statementValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void StatementValidator_EmptyStudentId_ShouldFailValidation()
    {
        // Arrange
        var request = new GenerateStatementRequest(
            StudentId: Guid.Empty,
            BillingPeriodStart: new DateOnly(2026, 1, 1),
            BillingPeriodEnd: new DateOnly(2026, 6, 30));

        // Act
        var result = _statementValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.StudentId));
    }

    [Fact]
    public void StatementValidator_StartDateAfterEndDate_ShouldFailValidation()
    {
        // Arrange
        var request = new GenerateStatementRequest(
            StudentId: Guid.NewGuid(),
            BillingPeriodStart: new DateOnly(2026, 7, 1),
            BillingPeriodEnd: new DateOnly(2026, 6, 30));

        // Act
        var result = _statementValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.BillingPeriodStart));
    }

    [Fact]
    public void BillingDocumentPaginatedList_Math_ShouldCalculatePagesCorrectly()
    {
        // Arrange
        var items = new List<BillingDocumentListItemDto>
        {
            new(
                Guid.NewGuid(),
                "BIL-2026-00001",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "STD-2026-001",
                "Ahmad Dahlan",
                "bill",
                "Bapak Ahmad",
                "Father",
                "08123456789",
                null,
                null,
                new DateOnly(2026, 11, 4),
                new DateOnly(2026, 11, 18),
                750000,
                "IDR",
                "portal",
                "issued",
                null,
                null,
                DateTimeOffset.UtcNow)
        };

        // Act
        var paginated = PaginatedList<BillingDocumentListItemDto>.Create(items, totalCount: 25, pageNumber: 2, pageSize: 10);

        // Assert
        Assert.Equal(2, paginated.PageNumber);
        Assert.Equal(10, paginated.PageSize);
        Assert.Equal(25, paginated.TotalCount);
        Assert.Equal(3, paginated.TotalPages);
        Assert.True(paginated.HasPreviousPage);
        Assert.True(paginated.HasNextPage);
    }

    [Fact]
    public void AddFinanceDomain_ShouldRegisterAllBillingHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory("Server=dummy;Database=dummy;"));
        services.AddScoped<ICurrentUser, StubCurrentUser>();
        services.AddFinanceDomain();

        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.NotNull(provider.GetService<GenerateBillingDocumentHandler>());
        Assert.NotNull(provider.GetService<GenerateStatementHandler>());
        Assert.NotNull(provider.GetService<GetBillingDocumentsHandler>());
        Assert.NotNull(provider.GetService<GetBillingDocumentByIdHandler>());
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? Id => Guid.NewGuid();
        public string? Username => "test-officer";
        public string? Email => "officer@test.elsasa.sch.id";
        public IReadOnlyList<string> Roles => ["FinanceOfficer"];
        public bool IsAuthenticated => true;
    }
}
