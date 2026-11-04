namespace Elsasa.Sis.Finance.Features.Payments.RejectPayment;

using FluentValidation;

/// <summary>
/// Validator enforcing mandatory rejection reason for payment rejection.
/// </summary>
public sealed class RejectPaymentValidator : AbstractValidator<RejectPaymentRequest>
{
    public RejectPaymentValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("Rejection reason is mandatory.")
            .MaximumLength(1000)
            .WithMessage("Rejection reason cannot exceed 1000 characters.");
    }
}
