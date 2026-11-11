namespace Elsasa.Sis.Teaching.Features.Substitutions.ArrangeEmergencySubstitution;

using FluentValidation;

/// <summary>
/// Validator for ArrangeEmergencySubstitutionRequest (TD-05, P4-S11).
/// </summary>
public sealed class ArrangeEmergencySubstitutionValidator : AbstractValidator<ArrangeEmergencySubstitutionRequest>
{
    public ArrangeEmergencySubstitutionValidator()
    {
        RuleFor(x => x.OriginalAssignmentId)
            .NotEmpty()
            .WithMessage("Original assignment ID is required.");

        RuleFor(x => x.SubstituteTeacherId)
            .NotEmpty()
            .WithMessage("Substitute teacher ID is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after start date.");

        RuleFor(x => x.CoverageScopeType)
            .NotEmpty()
            .WithMessage("Coverage scope type is required.")
            .Must(s => string.Equals(s, "full", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(s, "partial", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Coverage scope type must be 'full' or 'partial'.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required.")
            .MaximumLength(50)
            .WithMessage("Reason must not exceed 50 characters.");

        RuleFor(x => x.EffectiveHours)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Covered workload hours must be non-negative.");
    }
}
