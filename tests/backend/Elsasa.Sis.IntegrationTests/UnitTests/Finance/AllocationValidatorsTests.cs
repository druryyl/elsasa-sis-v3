namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;
using Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;
using FluentValidation.TestHelper;

/// <summary>
/// Unit tests for allocation validators:
/// <see cref="AllocatePaymentValidator"/> and <see cref="ReallocatePaymentValidator"/> (TD-05, OQ-003).
/// </summary>
public sealed class AllocationValidatorsTests
{
    private readonly AllocatePaymentValidator _allocateValidator = new();
    private readonly ReallocatePaymentValidator _reallocateValidator = new();

    #region AllocatePaymentValidator Tests

    [Fact]
    public void AllocatePayment_ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var request = new AllocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            Allocations:
            [
                new AllocationItemRequest(Guid.NewGuid(), 3_000_000, "Enrollment fee payment"),
                new AllocationItemRequest(Guid.NewGuid(), 1_500_000, "Tuition fee payment")
            ],
            Notes: "Standard cashier counter allocation");

        // Act
        var result = _allocateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AllocatePayment_EmptyPaymentId_ShouldFailValidation()
    {
        // Arrange
        var request = new AllocatePaymentRequest(
            PaymentId: Guid.Empty,
            Allocations: [new AllocationItemRequest(Guid.NewGuid(), 1_000_000)]);

        // Act
        var result = _allocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentId)
            .WithErrorMessage("PaymentId is required.");
    }

    [Fact]
    public void AllocatePayment_EmptyAllocationsList_ShouldFailValidation()
    {
        // Arrange
        var request = new AllocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            Allocations: []);

        // Act
        var result = _allocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Allocations)
            .WithErrorMessage("At least one allocation item must be specified.");
    }

    [Fact]
    public void AllocatePayment_DuplicateObligationIds_ShouldFailValidation()
    {
        // Arrange
        var obligationId = Guid.NewGuid();
        var request = new AllocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            Allocations:
            [
                new AllocationItemRequest(obligationId, 1_000_000),
                new AllocationItemRequest(obligationId, 2_000_000)
            ]);

        // Act
        var result = _allocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Allocations)
            .WithErrorMessage("Duplicate obligation IDs are not permitted in the same allocation request.");
    }

    [Fact]
    public void AllocatePayment_EmptyObligationIdInItem_ShouldFailValidation()
    {
        // Arrange
        var request = new AllocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            Allocations: [new AllocationItemRequest(Guid.Empty, 1_000_000)]);

        // Act
        var result = _allocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Allocations[0].ObligationId")
            .WithErrorMessage("ObligationId is required for each allocation.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500_000)]
    public void AllocatePayment_NonPositiveAmountInItem_ShouldFailValidation(long invalidAmount)
    {
        // Arrange
        var request = new AllocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            Allocations: [new AllocationItemRequest(Guid.NewGuid(), invalidAmount)]);

        // Act
        var result = _allocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Allocations[0].Amount")
            .WithErrorMessage("Allocated amount must be greater than zero.");
    }

    [Fact]
    public void AllocatePayment_NotesExceedingLimits_ShouldFailValidation()
    {
        // Arrange
        var itemLongNotes = new string('A', 1001);
        var reqLongNotes = new string('B', 2001);

        var request = new AllocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            Allocations: [new AllocationItemRequest(Guid.NewGuid(), 1_000_000, itemLongNotes)],
            Notes: reqLongNotes);

        // Act
        var result = _allocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Allocations[0].Notes");
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    #endregion

    #region ReallocatePaymentValidator Tests

    [Fact]
    public void ReallocatePayment_ValidRequest_WithReplacements_ShouldPassValidation()
    {
        // Arrange
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [Guid.NewGuid(), Guid.NewGuid()],
            ReplacementAllocations:
            [
                new ReplacementAllocationItemRequest(Guid.NewGuid(), 2_000_000, "Replacement for tuition"),
                new ReplacementAllocationItemRequest(Guid.NewGuid(), 1_000_000, "Replacement for books")
            ],
            ReversalReason: "Payer requested redirecting funds from extracurricular fee to tuition obligation.",
            Notes: "Approved by finance manager");

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReallocatePayment_ValidRequest_PureReversalWithoutReplacements_ShouldPassValidation()
    {
        // Arrange
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [Guid.NewGuid()],
            ReplacementAllocations: null,
            ReversalReason: "Payment allocated in error to wrong student account; reversing to unallocated balance.");

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReallocatePayment_EmptyPaymentId_ShouldFailValidation()
    {
        // Arrange
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.Empty,
            PriorAllocationIds: [Guid.NewGuid()],
            ReplacementAllocations: null,
            ReversalReason: "Valid reason for reversal");

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentId)
            .WithErrorMessage("PaymentId is required.");
    }

    [Fact]
    public void ReallocatePayment_EmptyPriorAllocationIds_ShouldFailValidation()
    {
        // Arrange
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [],
            ReplacementAllocations: null,
            ReversalReason: "Valid reversal reason");

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PriorAllocationIds)
            .WithErrorMessage("At least one prior allocation ID must be specified for reallocation or reversal.");
    }

    [Fact]
    public void ReallocatePayment_DuplicatePriorAllocationIds_ShouldFailValidation()
    {
        // Arrange
        var priorId = Guid.NewGuid();
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [priorId, priorId],
            ReplacementAllocations: null,
            ReversalReason: "Valid reversal reason");

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PriorAllocationIds)
            .WithErrorMessage("Duplicate prior allocation IDs are not permitted.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReallocatePayment_EmptyReversalReason_ShouldFailValidation(string? invalidReason)
    {
        // Arrange
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [Guid.NewGuid()],
            ReplacementAllocations: null,
            ReversalReason: invalidReason!);

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReversalReason)
            .WithErrorMessage("A mandatory reversal reason must be provided for audit tracking.");
    }

    [Fact]
    public void ReallocatePayment_ReversalReasonTooLong_ShouldFailValidation()
    {
        // Arrange
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [Guid.NewGuid()],
            ReplacementAllocations: null,
            ReversalReason: new string('Z', 1001));

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReversalReason)
            .WithErrorMessage("Reversal reason must not exceed 1000 characters.");
    }

    [Fact]
    public void ReallocatePayment_DuplicateObligationIdsInReplacement_ShouldFailValidation()
    {
        // Arrange
        var obligationId = Guid.NewGuid();
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [Guid.NewGuid()],
            ReplacementAllocations:
            [
                new ReplacementAllocationItemRequest(obligationId, 1_000_000),
                new ReplacementAllocationItemRequest(obligationId, 2_000_000)
            ],
            ReversalReason: "Valid reversal reason");

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReplacementAllocations)
            .WithErrorMessage("Duplicate obligation IDs are not permitted in replacement allocations.");
    }

    [Fact]
    public void ReallocatePayment_EmptyObligationIdInReplacement_ShouldFailValidation()
    {
        // Arrange
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [Guid.NewGuid()],
            ReplacementAllocations: [new ReplacementAllocationItemRequest(Guid.Empty, 1_000_000)],
            ReversalReason: "Valid reversal reason");

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("ReplacementAllocations[0].ObligationId")
            .WithErrorMessage("ObligationId is required for each replacement allocation.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void ReallocatePayment_NonPositiveAmountInReplacement_ShouldFailValidation(long invalidAmount)
    {
        // Arrange
        var request = new ReallocatePaymentRequest(
            PaymentId: Guid.NewGuid(),
            PriorAllocationIds: [Guid.NewGuid()],
            ReplacementAllocations: [new ReplacementAllocationItemRequest(Guid.NewGuid(), invalidAmount)],
            ReversalReason: "Valid reversal reason");

        // Act
        var result = _reallocateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("ReplacementAllocations[0].Amount")
            .WithErrorMessage("Replacement allocation amount must be greater than zero.");
    }

    #endregion
}
