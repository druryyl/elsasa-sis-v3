namespace Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;

using FluentValidation;

/// <summary>
/// Validator for financial policy approval request payload.
/// </summary>
public sealed class ApproveFinancialPolicyValidator : AbstractValidator<ApproveFinancialPolicyRequest>
{
    public ApproveFinancialPolicyValidator()
    {
        RuleFor(x => x.ApprovingAuthority)
            .MaximumLength(100).WithMessage("Approving authority must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ApprovingAuthority));
    }
}
