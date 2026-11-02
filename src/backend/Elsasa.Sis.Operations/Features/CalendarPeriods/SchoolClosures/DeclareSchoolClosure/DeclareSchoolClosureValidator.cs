namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.DeclareSchoolClosure;

using FluentValidation;

/// <summary>
/// Validator for declaring a school closure request (UC-OPS-12).
/// </summary>
public sealed class DeclareSchoolClosureValidator : AbstractValidator<DeclareSchoolClosureRequest>
{
    private static readonly HashSet<string> _allowedReasonCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "weather_disaster",
        "infrastructure_hazard",
        "health_safety",
        "government_mandate",
        "institutional_policy",
        "emergency",
    };

    private static readonly HashSet<string> _allowedAffectedPopulations = new(StringComparer.OrdinalIgnoreCase)
    {
        "all",
        "students_only",
        "staff_and_students",
    };

    public DeclareSchoolClosureValidator()
    {
        RuleFor(x => x.ReasonCategory)
            .NotEmpty().WithMessage("Reason category is required.")
            .Must(cat => cat is not null && _allowedReasonCategories.Contains(cat.Trim()))
            .WithMessage("Reason category must be one of: weather_disaster, infrastructure_hazard, health_safety, government_mandate, institutional_policy, emergency.");

        RuleFor(x => x.ReasonDetails)
            .NotEmpty().WithMessage("Reason details are required.")
            .MinimumLength(5).WithMessage("Reason details must be at least 5 characters.")
            .MaximumLength(2000).WithMessage("Reason details cannot exceed 2000 characters.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");

        When(x => !string.IsNullOrWhiteSpace(x.AffectedCampus), () =>
        {
            RuleFor(x => x.AffectedCampus!)
                .MaximumLength(100).WithMessage("Affected campus cannot exceed 100 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.AffectedPopulation), () =>
        {
            RuleFor(x => x.AffectedPopulation!)
                .Must(pop => _allowedAffectedPopulations.Contains(pop.Trim()))
                .WithMessage("Affected population must be one of: all, students_only, staff_and_students.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.AuthorizedBy), () =>
        {
            RuleFor(x => x.AuthorizedBy!)
                .MaximumLength(100).WithMessage("Authorized by cannot exceed 100 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.RecoveryPlanNotes), () =>
        {
            RuleFor(x => x.RecoveryPlanNotes!)
                .MaximumLength(2000).WithMessage("Recovery plan notes cannot exceed 2000 characters.");
        });
    }
}
