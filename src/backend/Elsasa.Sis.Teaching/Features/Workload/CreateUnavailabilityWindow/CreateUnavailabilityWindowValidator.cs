namespace Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow;

using FluentValidation;

/// <summary>
/// Validator for CreateUnavailabilityWindowRequest (P3-S08).
/// </summary>
public sealed class CreateUnavailabilityWindowValidator : AbstractValidator<CreateUnavailabilityWindowRequest>
{
    private static readonly string[] _validTypes = ["medical_leave", "official_training", "personal_leave", "blackout"];

    public CreateUnavailabilityWindowValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .Must(d => d != default).WithMessage("Start date must be a valid date.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after start date.");

        RuleFor(x => x.UnavailabilityType)
            .NotEmpty().WithMessage("Unavailability type is required.")
            .Must(t => _validTypes.Contains(t?.Trim().ToLowerInvariant()))
            .WithMessage("Unavailability type must be one of: 'medical_leave', 'official_training', 'personal_leave', 'blackout'.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(2000).WithMessage("Reason cannot exceed 2000 characters.");

        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime!.Value)
            .WithMessage("End time must be on or after start time.")
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);

        RuleFor(x => x.ExternalSourceReference)
            .MaximumLength(100).WithMessage("External source reference cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ExternalSourceReference));
    }
}
