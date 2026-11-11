namespace Elsasa.Sis.Teaching.Features.Workload.RequestWorkloadException;

using FluentValidation;

/// <summary>
/// FluentValidation validator for <see cref="RequestWorkloadExceptionRequest"/> (TD-04, P3-S10).
/// </summary>
public sealed class RequestWorkloadExceptionValidator : AbstractValidator<RequestWorkloadExceptionRequest>
{
    public RequestWorkloadExceptionValidator()
    {
        RuleFor(x => x.TeacherProfileId)
            .NotEmpty()
            .WithMessage("Teacher profile ID is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty()
            .WithMessage("Academic period ID is required.");

        RuleFor(x => x.RequestedOverloadHours)
            .GreaterThan(0)
            .WithMessage("Requested overload hours must be greater than zero.")
            .LessThanOrEqualTo(80)
            .WithMessage("Requested overload hours cannot exceed 80 hours per week.");

        RuleFor(x => x.CoordinatorJustification)
            .NotEmpty()
            .WithMessage("Coordinator justification is required.")
            .MaximumLength(2000)
            .WithMessage("Coordinator justification cannot exceed 2000 characters.");

        When(x => x.EffectiveStartDate.HasValue && x.EffectiveEndDate.HasValue, () =>
        {
            RuleFor(x => x.EffectiveEndDate)
                .GreaterThanOrEqualTo(x => x.EffectiveStartDate)
                .WithMessage("Effective end date cannot precede effective start date.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
        {
            RuleFor(x => x.Status)
                .Must(s => string.Equals(s, "requested", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(s, "recommended", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Status must be either 'requested' or 'recommended'.");
        });
    }
}
