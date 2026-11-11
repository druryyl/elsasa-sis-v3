namespace Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability;

using FluentValidation;

/// <summary>
/// Validator for DeclareTeacherAvailabilityRequest (P3-S08).
/// </summary>
public sealed class DeclareTeacherAvailabilityValidator : AbstractValidator<DeclareTeacherAvailabilityRequest>
{
    public DeclareTeacherAvailabilityValidator()
    {
        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.MaxAvailableHours)
            .GreaterThan(0).WithMessage("Maximum available hours must be greater than 0.")
            .LessThanOrEqualTo(168).WithMessage("Maximum available hours cannot exceed 168 hours per week.");

        RuleFor(x => x.EffectiveStartDate)
            .NotEmpty().WithMessage("Effective start date is required.")
            .Must(d => d != default).WithMessage("Effective start date must be a valid date.");

        RuleFor(x => x.EffectiveEndDate)
            .NotEmpty().WithMessage("Effective end date is required.")
            .GreaterThanOrEqualTo(x => x.EffectiveStartDate)
            .WithMessage("Effective end date must be on or after effective start date.");

        RuleFor(x => x.CampusScope)
            .MaximumLength(100).WithMessage("Campus scope cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CampusScope));

        RuleFor(x => x.AvailableDeliveryModes)
            .MaximumLength(100).WithMessage("Available delivery modes cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.AvailableDeliveryModes));
    }
}
