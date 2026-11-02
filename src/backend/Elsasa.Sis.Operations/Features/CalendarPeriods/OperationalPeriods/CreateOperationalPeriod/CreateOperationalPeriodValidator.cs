namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;

using FluentValidation;

/// <summary>
/// Validates <see cref="CreateOperationalPeriodRequest"/> payload rules before processing (ADR-005).
/// </summary>
public sealed class CreateOperationalPeriodValidator : AbstractValidator<CreateOperationalPeriodRequest>
{
    private static readonly HashSet<string> _allowedPeriodTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "semester",
        "term",
        "quarter",
        "admission_window",
        "assessment_window",
        "reporting_period",
        "event_period",
        "custom"
    };

    public CreateOperationalPeriodValidator()
    {
        RuleFor(x => x.PeriodCode)
            .NotEmpty().WithMessage("Period code is required.")
            .MaximumLength(64).WithMessage("Period code cannot exceed 64 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.PeriodType)
            .NotEmpty().WithMessage("Period type is required.")
            .MaximumLength(32).WithMessage("Period type cannot exceed 32 characters.")
            .Must(_allowedPeriodTypes.Contains).WithMessage($"Period type must be one of: {string.Join(", ", _allowedPeriodTypes)}.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.CampusContext)
            .MaximumLength(100).WithMessage("Campus context cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.CampusContext));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
