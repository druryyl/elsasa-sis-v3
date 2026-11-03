namespace Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;

using FluentValidation;

/// <summary>
/// Validator for <see cref="EnrollAcceptedApplicantRequest"/> enforcing placement and payment gate rules (GAP-01, OQ-003).
/// </summary>
public sealed class EnrollAcceptedApplicantValidator : AbstractValidator<EnrollAcceptedApplicantRequest>
{
    public EnrollAcceptedApplicantValidator()
    {
        RuleFor(x => x.ApplicantId)
            .NotEmpty()
            .WithMessage("Applicant ID is required.");

        RuleFor(x => x.InitialCampus)
            .NotEmpty()
            .WithMessage("Initial campus is required.")
            .MaximumLength(100)
            .WithMessage("Initial campus must not exceed 100 characters.");

        RuleFor(x => x.InitialLevel)
            .NotEmpty()
            .WithMessage("Initial level is required.")
            .MaximumLength(64)
            .WithMessage("Initial level must not exceed 64 characters.");

        RuleFor(x => x.InitialAcademicYear)
            .NotEmpty()
            .WithMessage("Initial academic year is required.")
            .MaximumLength(32)
            .WithMessage("Initial academic year must not exceed 32 characters.");

        When(x => x.PaymentWaived, () =>
        {
            RuleFor(x => x.WaiverReference)
                .NotEmpty()
                .WithMessage("Waiver reference is required when payment is waived.")
                .MaximumLength(100)
                .WithMessage("Waiver reference must not exceed 100 characters.");
        });

        When(x => !x.PaymentWaived && x.CheckFinanceModule != true, () =>
        {
            RuleFor(x => x.PaymentReference)
                .NotEmpty()
                .WithMessage("Payment reference is required when payment is not waived and automated finance check is not requested.")
                .MaximumLength(100)
                .WithMessage("Payment reference must not exceed 100 characters.");
        });
    }
}
