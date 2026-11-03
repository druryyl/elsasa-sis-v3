namespace Elsasa.Sis.Students.Features.Admission.WithdrawAdmissionApplication;

using FluentValidation;

/// <summary>
/// Validates <see cref="WithdrawAdmissionApplicationRequest"/> before execution (ADR-005).
/// </summary>
public sealed class WithdrawAdmissionApplicationValidator : AbstractValidator<WithdrawAdmissionApplicationRequest>
{
    public WithdrawAdmissionApplicationValidator()
    {
        RuleFor(x => x.WithdrawalReason)
            .NotEmpty().WithMessage("Withdrawal reason is required.")
            .MaximumLength(500).WithMessage("Withdrawal reason cannot exceed 500 characters.");
    }
}
