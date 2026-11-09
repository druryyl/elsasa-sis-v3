namespace Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;

using FluentValidation;

/// <summary>
/// Validator enforcing domain invariants for intervention status transitions (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class UpdateInterventionStatusRequestValidator : AbstractValidator<UpdateInterventionStatusRequest>
{
    private static readonly HashSet<string> _validStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "planned",
        "in_progress",
        "completed",
        "cancelled"
    };

    public UpdateInterventionStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => _validStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}.");

        When(x => string.Equals(x.Status, "completed", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.OutcomeNotes)
                .NotEmpty().WithMessage("Outcome notes are required when completing an intervention.")
                .MinimumLength(5).WithMessage("Outcome notes must be at least 5 characters.")
                .MaximumLength(2000).WithMessage("Outcome notes cannot exceed 2000 characters.");
        });

        When(x => !string.IsNullOrEmpty(x.OutcomeNotes), () =>
        {
            RuleFor(x => x.OutcomeNotes)
                .MaximumLength(2000).WithMessage("Outcome notes cannot exceed 2000 characters.");
        });
    }
}

/// <summary>
/// Validator for the command representation of intervention status transition.
/// </summary>
public sealed class UpdateInterventionStatusCommandValidator : AbstractValidator<UpdateInterventionStatusCommand>
{
    private static readonly HashSet<string> _validStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "planned",
        "in_progress",
        "completed",
        "cancelled"
    };

    public UpdateInterventionStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Intervention ID is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => _validStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}.");

        When(x => string.Equals(x.Status, "completed", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.OutcomeNotes)
                .NotEmpty().WithMessage("Outcome notes are required when completing an intervention.")
                .MinimumLength(5).WithMessage("Outcome notes must be at least 5 characters.")
                .MaximumLength(2000).WithMessage("Outcome notes cannot exceed 2000 characters.");
        });

        When(x => !string.IsNullOrEmpty(x.OutcomeNotes), () =>
        {
            RuleFor(x => x.OutcomeNotes)
                .MaximumLength(2000).WithMessage("Outcome notes cannot exceed 2000 characters.");
        });
    }
}
