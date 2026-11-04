namespace Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;

using FluentValidation;

/// <summary>
/// FluentValidation validator for ReallocatePaymentRequest (TD-05, OQ-003).
/// </summary>
public sealed class ReallocatePaymentValidator : AbstractValidator<ReallocatePaymentRequest>
{
    public ReallocatePaymentValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("PaymentId is required.");

        RuleFor(x => x.PriorAllocationIds)
            .NotEmpty()
            .WithMessage("At least one prior allocation ID must be specified for reallocation or reversal.");

        RuleFor(x => x.PriorAllocationIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate prior allocation IDs are not permitted.");

        RuleFor(x => x.ReversalReason)
            .NotEmpty()
            .WithMessage("A mandatory reversal reason must be provided for audit tracking.")
            .MaximumLength(1000)
            .WithMessage("Reversal reason must not exceed 1000 characters.");

        RuleFor(x => x.ReplacementAllocations)
            .Must(items => items == null || items.Select(i => i.ObligationId).Distinct().Count() == items.Count)
            .WithMessage("Duplicate obligation IDs are not permitted in replacement allocations.");

        RuleForEach(x => x.ReplacementAllocations)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ObligationId)
                    .NotEmpty()
                    .WithMessage("ObligationId is required for each replacement allocation.");

                item.RuleFor(i => i.Amount)
                    .GreaterThan(0)
                    .WithMessage("Replacement allocation amount must be greater than zero.");

                item.RuleFor(i => i.Notes)
                    .MaximumLength(1000)
                    .When(i => !string.IsNullOrEmpty(i.Notes));
            });

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
