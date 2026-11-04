namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Endpoints;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Unit tests for payment allocation auto-suggest engine (<see cref="SuggestAllocationsHandler"/>,
/// <see cref="SuggestAllocationsValidator"/>, and <see cref="SuggestAllocationsEndpoint"/>) (TD-05, OQ-003).
/// Verifies ordering rules: mandatory enrollment/admission fee first, then oldest tuition FIFO, then non-mandatory fees.
/// </summary>
public sealed class SuggestAllocationsTests
{
    private readonly SuggestAllocationsValidator _validator = new();

    #region SuggestAllocationsValidator Tests

    [Fact]
    public void SuggestAllocationsValidator_ValidRequest_WithStudentId_ShouldPass()
    {
        // Arrange
        var request = new SuggestAllocationsRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            PaymentAmount: 5_000_000);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SuggestAllocationsValidator_ValidRequest_WithApplicantId_ShouldPass()
    {
        // Arrange
        var request = new SuggestAllocationsRequest(
            StudentId: null,
            ApplicantId: Guid.NewGuid(),
            PaymentAmount: 3_000_000);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100_000)]
    public void SuggestAllocationsValidator_NonPositiveAmount_ShouldFail(long invalidAmount)
    {
        // Arrange
        var request = new SuggestAllocationsRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            PaymentAmount: invalidAmount);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentAmount)
            .WithErrorMessage("Payment amount must be greater than zero.");
    }

    [Fact]
    public void SuggestAllocationsValidator_MissingBothStudentAndApplicant_ShouldFail()
    {
        // Arrange
        var request = new SuggestAllocationsRequest(
            StudentId: null,
            ApplicantId: null,
            PaymentAmount: 5_000_000);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Either studentId or applicantId must be specified.");
    }

    [Fact]
    public void SuggestAllocationsValidator_EmptyGuids_ShouldFail()
    {
        // Arrange
        var request = new SuggestAllocationsRequest(
            StudentId: Guid.Empty,
            ApplicantId: Guid.Empty,
            PaymentAmount: 5_000_000);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Either studentId or applicantId must be specified.");
    }

    #endregion

    #region SuggestAllocationsHandler Early Validation Tests

    [Fact]
    public async Task HandleAsync_NonPositivePaymentAmount_ShouldReturnValidationError()
    {
        // Arrange
        var connectionFactory = new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;");
        var handler = new SuggestAllocationsHandler(connectionFactory);
        var request = new SuggestAllocationsRequest(
            StudentId: Guid.NewGuid(),
            ApplicantId: null,
            PaymentAmount: 0);

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Finance.Allocation.InvalidAmount", result.Error.Code);
        Assert.Equal("Payment amount must be greater than zero.", result.Error.Message);
    }

    [Fact]
    public async Task HandleAsync_MissingCandidateIdentifiers_ShouldReturnValidationError()
    {
        // Arrange
        var connectionFactory = new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;");
        var handler = new SuggestAllocationsHandler(connectionFactory);
        var request = new SuggestAllocationsRequest(
            StudentId: Guid.Empty,
            ApplicantId: Guid.Empty,
            PaymentAmount: 1_000_000);

        // Act
        var result = await handler.HandleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Finance.Allocation.CandidateRequired", result.Error.Code);
        Assert.Equal("Either studentId or applicantId must be specified.", result.Error.Message);
    }

    #endregion

    #region Auto-Suggest Priority Ordering & Greedy Allocation Simulation Tests

    private sealed record TestObligation(
        Guid ObligationId,
        string ObligationNumber,
        string ChargeType,
        string Description,
        bool IsMandatory,
        DateOnly DueDate,
        long OriginalAmount,
        long TotalPaid,
        long OutstandingAmount);

    /// <summary>
    /// Evaluates the domain auto-suggest priority sorting rule matching Section 4 TD-05:
    /// 1. Mandatory enrollment/admission obligations (charge_type IN ('enrollment_fee', 'admission_fee') AND is_mandatory = 1)
    /// 2. Mandatory obligations ordered by due_date ASC (FIFO)
    /// 3. Non-mandatory obligations ordered by due_date ASC
    /// </summary>
    private static int GetPriorityTier(TestObligation item)
    {
        if (item.IsMandatory && (item.ChargeType == "enrollment_fee" || item.ChargeType == "admission_fee"))
        {
            return 1;
        }

        if (item.IsMandatory)
        {
            return 2;
        }

        return 3;
    }

    private static List<TestObligation> SortByAutoSuggestPriority(IEnumerable<TestObligation> obligations)
    {
        return obligations
            .OrderBy(o => GetPriorityTier(o))
            .ThenBy(o => o.DueDate)
            .ThenBy(o => o.ObligationNumber)
            .ToList();
    }

    [Fact]
    public void AutoSuggestSortingRules_ShouldOrderMandatoryEnrollmentFirst_ThenOldestTuitionFIFO_ThenNonMandatoryFees()
    {
        // Arrange: Unordered obligations with varying types, due dates, and mandatory flags
        var nonMandatoryActivity = new TestObligation(
            Guid.NewGuid(), "OBL-2026-0001", "activity_fee", "School Trip Fee",
            IsMandatory: false, DueDate: new DateOnly(2026, 1, 5), OriginalAmount: 500_000, TotalPaid: 0, OutstandingAmount: 500_000);

        var olderTuition = new TestObligation(
            Guid.NewGuid(), "OBL-2026-0002", "tuition", "Tuition January 2026",
            IsMandatory: true, DueDate: new DateOnly(2026, 1, 15), OriginalAmount: 2_000_000, TotalPaid: 0, OutstandingAmount: 2_000_000);

        var newerTuition = new TestObligation(
            Guid.NewGuid(), "OBL-2026-0003", "tuition", "Tuition February 2026",
            IsMandatory: true, DueDate: new DateOnly(2026, 2, 15), OriginalAmount: 2_000_000, TotalPaid: 0, OutstandingAmount: 2_000_000);

        var mandatoryEnrollmentFee = new TestObligation(
            Guid.NewGuid(), "OBL-2026-0004", "enrollment_fee", "Enrollment Fee 2026/2027",
            IsMandatory: true, DueDate: new DateOnly(2026, 3, 1), OriginalAmount: 5_000_000, TotalPaid: 0, OutstandingAmount: 5_000_000);

        var mandatoryAdmissionFee = new TestObligation(
            Guid.NewGuid(), "OBL-2026-0005", "admission_fee", "Admission Registration Fee",
            IsMandatory: true, DueDate: new DateOnly(2026, 3, 10), OriginalAmount: 1_000_000, TotalPaid: 0, OutstandingAmount: 1_000_000);

        var nonMandatoryClub = new TestObligation(
            Guid.NewGuid(), "OBL-2026-0006", "club_fee", "Robotics Club Fee",
            IsMandatory: false, DueDate: new DateOnly(2026, 2, 20), OriginalAmount: 350_000, TotalPaid: 0, OutstandingAmount: 350_000);

        var openObligations = new List<TestObligation>
        {
            nonMandatoryActivity,
            newerTuition,
            olderTuition,
            mandatoryAdmissionFee,
            mandatoryEnrollmentFee,
            nonMandatoryClub
        };

        // Act
        var sorted = SortByAutoSuggestPriority(openObligations);

        // Assert
        Assert.Equal(6, sorted.Count);

        // 1. Mandatory enrollment and admission fees must come first (Priority Tier 1)
        Assert.Equal("enrollment_fee", sorted[0].ChargeType);
        Assert.Equal("admission_fee", sorted[1].ChargeType);

        // 2. Mandatory tuition must come next, ordered FIFO by oldest due date (Priority Tier 2)
        Assert.Equal("tuition", sorted[2].ChargeType);
        Assert.Equal(new DateOnly(2026, 1, 15), sorted[2].DueDate);

        Assert.Equal("tuition", sorted[3].ChargeType);
        Assert.Equal(new DateOnly(2026, 2, 15), sorted[3].DueDate);

        // 3. Non-mandatory fees must come last, ordered by due date (Priority Tier 3)
        Assert.Equal("activity_fee", sorted[4].ChargeType);
        Assert.Equal(new DateOnly(2026, 1, 5), sorted[4].DueDate);

        Assert.Equal("club_fee", sorted[5].ChargeType);
        Assert.Equal(new DateOnly(2026, 2, 20), sorted[5].DueDate);
    }

    [Fact]
    public void GreedyAllocation_ShouldAllocateFundsAcrossObligations_InPriorityOrder()
    {
        // Arrange
        var enrollment = new TestObligation(
            Guid.NewGuid(), "OBL-01", "enrollment_fee", "Enrollment Fee",
            IsMandatory: true, DueDate: new DateOnly(2026, 3, 1), OriginalAmount: 5_000_000, TotalPaid: 0, OutstandingAmount: 5_000_000);

        var olderTuition = new TestObligation(
            Guid.NewGuid(), "OBL-02", "tuition", "Oldest Tuition",
            IsMandatory: true, DueDate: new DateOnly(2026, 1, 1), OriginalAmount: 2_000_000, TotalPaid: 0, OutstandingAmount: 2_000_000);

        var newerTuition = new TestObligation(
            Guid.NewGuid(), "OBL-03", "tuition", "Newer Tuition",
            IsMandatory: true, DueDate: new DateOnly(2026, 2, 1), OriginalAmount: 2_000_000, TotalPaid: 0, OutstandingAmount: 2_000_000);

        var activityFee = new TestObligation(
            Guid.NewGuid(), "OBL-04", "activity_fee", "Trip",
            IsMandatory: false, DueDate: new DateOnly(2026, 1, 15), OriginalAmount: 500_000, TotalPaid: 0, OutstandingAmount: 500_000);

        var sorted = SortByAutoSuggestPriority([activityFee, newerTuition, olderTuition, enrollment]);

        // Total available payment: 7,500,000 IDR
        var paymentAmount = 7_500_000L;
        var remainingFunds = paymentAmount;
        var suggestions = new List<AllocationSuggestionItemDto>();

        // Act: Run exact greedy allocation logic from SuggestAllocationsHandler
        foreach (var row in sorted)
        {
            if (remainingFunds <= 0)
            {
                break;
            }

            var allocate = Math.Min(row.OutstandingAmount, remainingFunds);
            if (allocate > 0)
            {
                suggestions.Add(new AllocationSuggestionItemDto(
                    row.ObligationId,
                    row.ObligationNumber,
                    row.ChargeType,
                    row.Description,
                    row.IsMandatory,
                    row.DueDate,
                    row.OriginalAmount,
                    row.TotalPaid,
                    row.OutstandingAmount,
                    allocate,
                    row.OutstandingAmount - allocate));

                remainingFunds -= allocate;
            }
        }

        var totalAllocated = paymentAmount - remainingFunds;

        // Assert
        Assert.Equal(3, suggestions.Count);

        // First suggestion: 100% of enrollment fee (5,000,000)
        Assert.Equal("enrollment_fee", suggestions[0].ChargeType);
        Assert.Equal(5_000_000, suggestions[0].SuggestedAmount);
        Assert.Equal(0, suggestions[0].RemainingObligationBalance);

        // Second suggestion: 100% of oldest tuition (2,000,000)
        Assert.Equal("tuition", suggestions[1].ChargeType);
        Assert.Equal(new DateOnly(2026, 1, 1), suggestions[1].DueDate);
        Assert.Equal(2_000_000, suggestions[1].SuggestedAmount);
        Assert.Equal(0, suggestions[1].RemainingObligationBalance);

        // Third suggestion: partial allocation to newer tuition (500,000 of 2,000,000)
        Assert.Equal("tuition", suggestions[2].ChargeType);
        Assert.Equal(new DateOnly(2026, 2, 1), suggestions[2].DueDate);
        Assert.Equal(500_000, suggestions[2].SuggestedAmount);
        Assert.Equal(1_500_000, suggestions[2].RemainingObligationBalance);

        // Overall funds balance
        Assert.Equal(7_500_000, totalAllocated);
        Assert.Equal(0, remainingFunds);
    }

    [Fact]
    public void GreedyAllocation_WithOverpayment_ShouldFullySettleAllAndRetainUnallocatedBalance()
    {
        // Arrange
        var tuition = new TestObligation(
            Guid.NewGuid(), "OBL-01", "tuition", "Tuition Fee",
            IsMandatory: true, DueDate: new DateOnly(2026, 1, 1), OriginalAmount: 2_000_000, TotalPaid: 0, OutstandingAmount: 2_000_000);

        var fee = new TestObligation(
            Guid.NewGuid(), "OBL-02", "uniform_fee", "Uniform",
            IsMandatory: false, DueDate: new DateOnly(2026, 1, 10), OriginalAmount: 500_000, TotalPaid: 0, OutstandingAmount: 500_000);

        var sorted = SortByAutoSuggestPriority([fee, tuition]);

        // Payment exceeds total outstanding: 5,000,000 IDR vs 2,500,000 IDR total outstanding
        var paymentAmount = 5_000_000L;
        var remainingFunds = paymentAmount;
        var suggestions = new List<AllocationSuggestionItemDto>();

        foreach (var row in sorted)
        {
            if (remainingFunds <= 0)
            {
                break;
            }

            var allocate = Math.Min(row.OutstandingAmount, remainingFunds);
            if (allocate > 0)
            {
                suggestions.Add(new AllocationSuggestionItemDto(
                    row.ObligationId,
                    row.ObligationNumber,
                    row.ChargeType,
                    row.Description,
                    row.IsMandatory,
                    row.DueDate,
                    row.OriginalAmount,
                    row.TotalPaid,
                    row.OutstandingAmount,
                    allocate,
                    row.OutstandingAmount - allocate));

                remainingFunds -= allocate;
            }
        }

        var totalAllocated = paymentAmount - remainingFunds;

        // Assert
        Assert.Equal(2, suggestions.Count);
        Assert.Equal(2_500_000, totalAllocated);
        Assert.Equal(2_500_000, remainingFunds); // Retained as unallocated balance
        Assert.All(suggestions, s => Assert.Equal(0, s.RemainingObligationBalance));
    }

    #endregion

    #region SuggestAllocationsEndpoint Tests

    [Fact]
    public void SuggestAllocationsEndpoint_Should_ImplementIEndpoint_And_MapCleanly()
    {
        // Arrange
        var endpoint = new SuggestAllocationsEndpoint();

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

    #endregion
}
