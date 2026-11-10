namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.ChangeTeacherStanding;

using FluentValidation;

/// <summary>
/// Validator for ChangeTeacherStandingRequest (P2-S06).
/// Validates standing is in ('active', 'restricted', 'suspended', 'inactive') and mandatory standing_reason.
/// </summary>
public sealed class ChangeTeacherStandingValidator : AbstractValidator<ChangeTeacherStandingRequest>
{
    private static readonly HashSet<string> _allowedStandings = new(StringComparer.OrdinalIgnoreCase)
    {
        "active",
        "restricted",
        "suspended",
        "inactive"
    };

    public ChangeTeacherStandingValidator()
    {
        RuleFor(x => x.Standing)
            .NotEmpty().WithMessage("Standing is required.")
            .Must(s => !string.IsNullOrWhiteSpace(s) && _allowedStandings.Contains(s.Trim()))
            .WithMessage("Standing must be one of: 'active', 'restricted', 'suspended', 'inactive'.");

        RuleFor(x => x.StandingReason)
            .NotEmpty().WithMessage("Standing justification reason is mandatory.")
            .MaximumLength(2000).WithMessage("Standing reason must not exceed 2000 characters.");
    }
}
