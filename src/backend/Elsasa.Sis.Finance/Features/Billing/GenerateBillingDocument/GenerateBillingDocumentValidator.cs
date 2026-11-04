namespace Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;

using FluentValidation;

/// <summary>
/// Validator for compile billing document request.
/// </summary>
public sealed class GenerateBillingDocumentValidator : AbstractValidator<GenerateBillingDocumentRequest>
{
    public GenerateBillingDocumentValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.PaymentDueDate)
            .Must(d => !d.HasValue || d.Value != default)
            .WithMessage("Payment due date cannot be a default date.")
            .When(x => x.PaymentDueDate.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
