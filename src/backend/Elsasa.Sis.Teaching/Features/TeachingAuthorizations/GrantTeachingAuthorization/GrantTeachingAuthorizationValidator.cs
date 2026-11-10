namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.GrantTeachingAuthorization;

using FluentValidation;

/// <summary>
/// Validator for granting teaching authorization requests (TD-05, P4-S11).
/// </summary>
public sealed class GrantTeachingAuthorizationValidator : AbstractValidator<GrantTeachingAuthorizationRequest>
{
    private static readonly string[] _allowedStatuses = ["active", "authorized_under_supervision"];
    private static readonly string[] _allowedSupervisionTypes = ["co_teaching", "periodic_observation", "lesson_plan_review", "general_mentorship"];

    public GrantTeachingAuthorizationValidator()
    {
        RuleFor(x => x.ResolvedScopeDescription)
            .NotEmpty()
            .WithMessage("Authorization scope description is required.")
            .MaximumLength(4000)
            .WithMessage("Authorization scope description cannot exceed 4000 characters.");

        RuleFor(x => x.ResolvedCampusScope)
            .MaximumLength(100)
            .WithMessage("Campus scope boundary cannot exceed 100 characters.");

        RuleFor(x => x.ResolvedProgramScope)
            .MaximumLength(100)
            .WithMessage("Educational program scope cannot exceed 100 characters.");

        RuleFor(x => x.ResolvedLevelScope)
            .MaximumLength(100)
            .WithMessage("Grade level scope cannot exceed 100 characters.");

        RuleFor(x => x.DeliveryModeScope)
            .MaximumLength(50)
            .WithMessage("Delivery mode scope cannot exceed 50 characters.");

        RuleFor(x => x.ResolvedEffectiveStartDate)
            .NotEmpty()
            .WithMessage("Effective start date is required.");

        RuleFor(x => x.ResolvedEffectiveEndDate)
            .Must((request, endDate) => !endDate.HasValue || endDate.Value >= request.ResolvedEffectiveStartDate)
            .WithMessage("Effective end date must be on or after the effective start date.");

        RuleFor(x => x.ResolvedStatus)
            .Must(status => _allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Authorization status must be either 'active' or 'authorized_under_supervision'.");

        // Supervision requirement rules when status is authorized_under_supervision
        When(x => string.Equals(x.ResolvedStatus, "authorized_under_supervision", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.ResolvedSupervision)
                .NotNull()
                .WithMessage("Supervision requirement details are mandatory when authorization status is 'authorized_under_supervision'.");

            When(x => x.ResolvedSupervision is not null, () =>
            {
                RuleFor(x => x.ResolvedSupervision!.SupervisionType)
                    .Must(type => !string.IsNullOrWhiteSpace(type) && _allowedSupervisionTypes.Contains(type.Trim(), StringComparer.OrdinalIgnoreCase))
                    .WithMessage("Supervision type must be one of: 'co_teaching', 'periodic_observation', 'lesson_plan_review', 'general_mentorship'.");

                RuleFor(x => x.ResolvedSupervision!.Description)
                    .NotEmpty()
                    .WithMessage("Supervision description is required.")
                    .MaximumLength(2000)
                    .WithMessage("Supervision description cannot exceed 2000 characters.");

                RuleFor(x => x.ResolvedSupervision!.StartDate)
                    .NotEmpty()
                    .WithMessage("Supervision start date is required.");

                RuleFor(x => x.ResolvedSupervision!.EndDate)
                    .Must((req, endDate) => !endDate.HasValue || endDate.Value >= req.ResolvedSupervision!.StartDate)
                    .WithMessage("Supervision end date must be on or after the supervision start date.");
            });
        });
    }
}
