namespace Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;

using FluentValidation;

/// <summary>
/// Validator enforcing domain integrity constraints on batch academic standing determinations (P3-S07, UC-ACD-17).
/// </summary>
public sealed class BatchDetermineCohortStandingValidator : AbstractValidator<BatchDetermineCohortStandingCommand>
{
    public BatchDetermineCohortStandingValidator()
    {
        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items list is required.")
            .NotEmpty().WithMessage("At least one standing determination item is required.");

        RuleForEach(x => x.Items)
            .SetValidator(new StandingItemDtoValidator());
    }
}

/// <summary>
/// Validator for individual standing items within a batch determination command (P3-S07, UC-ACD-17).
/// </summary>
public sealed class StandingItemDtoValidator : AbstractValidator<StandingItemDto>
{
    private static readonly string[] _validStandings = ["good_standing", "conditional_standing", "not_in_good_standing"];

    public StandingItemDtoValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

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
