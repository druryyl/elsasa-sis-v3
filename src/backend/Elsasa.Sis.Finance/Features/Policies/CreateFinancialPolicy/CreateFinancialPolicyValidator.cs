namespace Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;

using FluentValidation;

/// <summary>
/// Validator for financial policy creation command enforcing format and business constraints.
/// </summary>
public sealed class CreateFinancialPolicyValidator : AbstractValidator<CreateFinancialPolicyRequest>
{
    public CreateFinancialPolicyValidator()
    {
        RuleFor(x => x.PolicyCode)
            .NotEmpty().WithMessage("Policy code is required.")
            .Matches(@"^FP-\d{4}-\d{3}$")
            .WithMessage("PolicyCode must follow the format 'FP-YYYY-NNN' (e.g. 'FP-2026-001').");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Policy name is required.")
            .MaximumLength(200).WithMessage("Policy name must not exceed 200 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(64).WithMessage("Category must not exceed 64 characters.");

        RuleFor(x => x.BusinessPurpose)
            .NotEmpty().WithMessage("Business purpose is required.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.EffectiveDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("EndDate must be on or after EffectiveDate.");

        RuleFor(x => x.PolicyVersion)
            .MaximumLength(32).WithMessage("Policy version must not exceed 32 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PolicyVersion));

        RuleFor(x => x.ApplicableContext)
            .MaximumLength(200).WithMessage("Applicable context must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ApplicableContext));
    }
}
