namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance;
using Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;
using Elsasa.Sis.Finance.Features.Obligations.GetObligations;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public sealed class AssessAdHocObligationValidatorTests
{
    private readonly AssessAdHocObligationValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: "library_fee",
            Description: "Replacement fee for damaged library textbook",
            Amount: 150000,
            DueDate: new DateOnly(2026, 12, 1),
            AcademicPeriodId: Guid.NewGuid(),
            Notes: "Assessed by library staff");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyStudentId_ShouldFailValidation()
    {
        // Arrange
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.Empty,
            ChargeType: "uniform_fee",
            Description: "Additional sports uniform set",
            Amount: 250000,
            DueDate: new DateOnly(2026, 12, 1),
            AcademicPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.StudentId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50000)]
    public void Validate_NonPositiveAmount_ShouldFailValidation(long amount)
    {
        // Arrange
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: "activity_fee",
            Description: "Field trip participation fee",
            Amount: amount,
            DueDate: new DateOnly(2026, 12, 1),
            AcademicPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Amount));
    }

    [Fact]
    public void Validate_DefaultDueDate_ShouldFailValidation()
    {
        // Arrange
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: "exam_fee",
            Description: "Remedial examination fee",
            Amount: 75000,
            DueDate: default,
            AcademicPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.DueDate));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyDescription_ShouldFailValidation(string description)
    {
        // Arrange
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: "fine",
            Description: description,
            Amount: 50000,
            DueDate: new DateOnly(2026, 12, 1),
            AcademicPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Description));
    }

    [Fact]
    public void Validate_DescriptionExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var longDescription = new string('x', 256);
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: "fine",
            Description: longDescription,
            Amount: 50000,
            DueDate: new DateOnly(2026, 12, 1),
            AcademicPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Description));
    }

    [Fact]
    public void Validate_PresentOrFutureDueDate_ShouldPassValidation()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: "fine",
            Description: "Valid ad-hoc charge",
            Amount: 50000,
            DueDate: today.AddDays(7),
            AcademicPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyChargeType_ShouldFailValidation(string? chargeType)
    {
        // Arrange
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: chargeType!,
            Description: "Valid ad-hoc charge",
            Amount: 50000,
            DueDate: new DateOnly(2026, 12, 1),
            AcademicPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.ChargeType));
    }

    [Fact]
    public void Validate_ChargeTypeExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: new string('T', 65),
            Description: "Valid ad-hoc charge",
            Amount: 50000,
            DueDate: new DateOnly(2026, 12, 1),
            AcademicPeriodId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.ChargeType));
    }

    [Fact]
    public void Validate_EmptyAcademicPeriodId_ShouldFailValidation()
    {
        // Arrange
        var request = new AssessAdHocObligationRequest(
            StudentId: Guid.NewGuid(),
            ChargeType: "fine",
            Description: "Valid ad-hoc charge",
            Amount: 50000,
            DueDate: new DateOnly(2026, 12, 1),
            AcademicPeriodId: Guid.Empty);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.AcademicPeriodId));
    }

    [Fact]
    public void GetObligationsResponse_Create_ShouldComputePaginationCorrectly()
    {
        // Arrange
        var items = new List<ObligationListItemDto>();
        for (int i = 0; i < 15; i++)
        {
            items.Add(new ObligationListItemDto(
                Guid.NewGuid(),
                $"OBL-2026-{i:D6}",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "STD-001",
                "Test Student",
                Guid.NewGuid(),
                "FP-2026-001",
                null,
                null,
                "Ad-hoc item",
                "other",
                true,
                100000,
                "IDR",
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 11, 15),
                Guid.NewGuid(),
                "Semester 1",
                "assessed",
                "SYSTEM",
                null,
                DateTimeOffset.UtcNow,
                []));
        }

        // Act
        var response = GetObligationsResponse.Create(items, totalCount: 45, pageNumber: 2, pageSize: 15);

        // Assert
        Assert.Equal(45, response.TotalCount);
        Assert.Equal(2, response.PageNumber);
        Assert.Equal(15, response.PageSize);
        Assert.Equal(3, response.TotalPages);
        Assert.True(response.HasPreviousPage);
        Assert.True(response.HasNextPage);
        Assert.Equal(15, response.Items.Count);
    }

    [Fact]
    public void AddFinanceDomain_Should_RegisterAdHocAndQueryHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;"));
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IPublisher>(new StubPublisher());

        // Act
        services.AddFinanceDomain();
        var provider = services.BuildServiceProvider();

        // Assert
        var assessHandler = provider.GetService<AssessAdHocObligationHandler>();
        Assert.NotNull(assessHandler);

        var queryHandler = provider.GetService<GetObligationsHandler>();
        Assert.NotNull(queryHandler);
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? Id => Guid.NewGuid();
        public string? Username => "TEST_FINANCE_OFFICER";
        public string? Email => "officer@test.elsasa.sch.id";
        public IReadOnlyList<string> Roles => ["FinanceOfficer"];
        public bool IsAuthenticated => true;
    }

    private sealed class StubPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }
}
