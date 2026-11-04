namespace Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;

using FluentValidation;

/// <summary>
/// FluentValidation validator for AllocatePaymentRequest (TD-05).
/// </summary>
public sealed class AllocatePaymentValidator : AbstractValidator<AllocatePaymentRequest>
{
    public AllocatePaymentValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("PaymentId is required.");

        RuleFor(x => x.Allocations)
            .NotEmpty()
            .WithMessage("At least one allocation item must be specified.");

        RuleFor(x => x.Allocations)
            .Must(allocations => allocations == null || allocations.Select(a => a.ObligationId).Distinct().Count() == allocations.Count)
            .WithMessage("Duplicate obligation IDs are not permitted in the same allocation request.");

        RuleForEach(x => x.Allocations)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ObligationId)
                    .NotEmpty()
                    .WithMessage("ObligationId is required for each allocation.");

                item.RuleFor(i => i.Amount)
                    .GreaterThan(0)
                    .WithMessage("Allocated amount must be greater than zero.");

                item.RuleFor(i => i.Notes)
                    .MaximumLength(1000)
                    .When(i => !string.IsNullOrEmpty(i.Notes));
            });

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
