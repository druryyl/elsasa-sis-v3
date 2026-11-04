namespace Elsasa.Sis.Finance.Features.Payments.SubmitPayment;

using FluentValidation;

/// <summary>
/// Validator asserting self-service payment intake parameters conform to business invariants.
/// </summary>
public sealed class SubmitPaymentValidator : AbstractValidator<SubmitPaymentRequest>
{
    public SubmitPaymentValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero IDR.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required.")
            .MaximumLength(64);

        RuleFor(x => x.PayerName)
            .NotEmpty()
            .WithMessage("Payer name is required.")
            .MaximumLength(200);

        RuleFor(x => x)
            .Must(x => (x.StudentId.HasValue && x.StudentId.Value != Guid.Empty)
                    || (x.ApplicantId.HasValue && x.ApplicantId.Value != Guid.Empty)
                    || (x.StudentAccountId.HasValue && x.StudentAccountId.Value != Guid.Empty))
            .WithMessage("At least one candidate identifier (StudentId, ApplicantId, or StudentAccountId) must be specified.");

        When(x => !string.IsNullOrWhiteSpace(x.PayerEmail), () =>
        {
            RuleFor(x => x.PayerEmail)
                .EmailAddress()
                .WithMessage("Payer email must be a valid email address.")
                .MaximumLength(200);
        });

        When(x => !string.IsNullOrWhiteSpace(x.ExternalReference), () =>
        {
            RuleFor(x => x.ExternalReference)
                .MaximumLength(100);
        });
    }
}
