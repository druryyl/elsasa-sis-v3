namespace Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;

using FluentValidation;

/// <summary>
/// Validator enforcing invariant rules for single-student ad-hoc obligation assessment (TD-04).
/// </summary>
public sealed class AssessAdHocObligationValidator : AbstractValidator<AssessAdHocObligationRequest>
{
    public AssessAdHocObligationValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("StudentId is required.");

        RuleFor(x => x.ChargeType)
            .NotEmpty()
            .WithMessage("ChargeType is required.")
            .MaximumLength(64)
            .WithMessage("ChargeType cannot exceed 64 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(255)
            .WithMessage("Description cannot exceed 255 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.DueDate)
            .NotEqual(default(DateOnly))
            .WithMessage("DueDate is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty()
            .WithMessage("AcademicPeriodId is required.");
    }
}
