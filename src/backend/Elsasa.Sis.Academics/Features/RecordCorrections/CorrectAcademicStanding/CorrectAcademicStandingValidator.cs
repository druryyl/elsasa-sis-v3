namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;

using FluentValidation;

/// <summary>
/// Validator for academic standing correction requests enforcing mandatory reason length and domain rules (P5-S12, UC-ACD-15).
/// </summary>
public sealed class CorrectAcademicStandingRequestValidator : AbstractValidator<CorrectAcademicStandingRequest>
{
    private static readonly string[] _validStandings = ["good_standing", "conditional_standing", "not_in_good_standing"];

    public CorrectAcademicStandingRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Correction reason is mandatory.")
            .MinimumLength(10).WithMessage("Correction reason must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Correction reason must not exceed 2000 characters.");

        RuleFor(x => x.Standing)
            .NotEmpty().WithMessage("Standing is required.")
            .Must(s => s is not null && _validStandings.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("Standing must be one of: good_standing, conditional_standing, not_in_good_standing.");

        RuleFor(x => x.PolicyReference)
            .NotEmpty().WithMessage("Policy reference is required.")
            .MaximumLength(100).WithMessage("Policy reference must not exceed 100 characters.");

        RuleFor(x => x.Conditions)
            .NotEmpty().WithMessage("Conditions are mandatory when standing is 'conditional_standing'.")
            .When(x => string.Equals(x.Standing?.Trim(), "conditional_standing", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Conditions)
            .MaximumLength(2000).WithMessage("Conditions must not exceed 2000 characters.");

        RuleFor(x => x.ExceptionReason)
            .NotEmpty().WithMessage("Exception reason is mandatory when IsException is true.")
            .MinimumLength(5).WithMessage("Exception reason must be at least 5 characters.")
            .MaximumLength(1000).WithMessage("Exception reason must not exceed 1000 characters.")
            .When(x => x.IsException);

        RuleFor(x => x.CommitteeReference)
            .NotEmpty().WithMessage("Committee reference is mandatory when IsException is true.")
            .MinimumLength(5).WithMessage("Committee reference must be at least 5 characters.")
            .MaximumLength(100).WithMessage("Committee reference must not exceed 100 characters.")
            .When(x => x.IsException);
    }
}

/// <summary>
/// Validator for academic standing correction command enforcing ID existence and invariants (P5-S12, UC-ACD-15).
/// </summary>
public sealed class CorrectAcademicStandingCommandValidator : AbstractValidator<CorrectAcademicStandingCommand>
{
    private static readonly string[] _validStandings = ["good_standing", "conditional_standing", "not_in_good_standing"];

    public CorrectAcademicStandingCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Standing ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Correction reason is mandatory.")
            .MinimumLength(10).WithMessage("Correction reason must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Correction reason must not exceed 2000 characters.");

        RuleFor(x => x.Standing)
            .NotEmpty().WithMessage("Standing is required.")
            .Must(s => s is not null && _validStandings.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("Standing must be one of: good_standing, conditional_standing, not_in_good_standing.");

        RuleFor(x => x.PolicyReference)
            .NotEmpty().WithMessage("Policy reference is required.")
            .MaximumLength(100).WithMessage("Policy reference must not exceed 100 characters.");

        RuleFor(x => x.Conditions)
            .NotEmpty().WithMessage("Conditions are mandatory when standing is 'conditional_standing'.")
            .When(x => string.Equals(x.Standing?.Trim(), "conditional_standing", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Conditions)
            .MaximumLength(2000).WithMessage("Conditions must not exceed 2000 characters.");

        RuleFor(x => x.ExceptionReason)
            .NotEmpty().WithMessage("Exception reason is mandatory when IsException is true.")
            .MinimumLength(5).WithMessage("Exception reason must be at least 5 characters.")
            .MaximumLength(1000).WithMessage("Exception reason must not exceed 1000 characters.")
            .When(x => x.IsException);

        RuleFor(x => x.CommitteeReference)
            .NotEmpty().WithMessage("Committee reference is mandatory when IsException is true.")
            .MinimumLength(5).WithMessage("Committee reference must be at least 5 characters.")
            .MaximumLength(100).WithMessage("Committee reference must not exceed 100 characters.")
            .When(x => x.IsException);
    }
}
