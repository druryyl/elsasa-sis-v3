namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ResolveCapacityConflict;

using FluentValidation;

/// <summary>
/// Validator for resolving a capacity conflict (TD-01, P3-S07).
/// </summary>
public sealed class ResolveCapacityConflictValidator : AbstractValidator<ResolveCapacityConflictRequest>
{
    private static readonly string[] _allowedStatuses =
    [
        "resolved",
        "waived_with_exception"
    ];

    public ResolveCapacityConflictValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Resolution status is required.")
            .Must(s => _allowedStatuses.Contains(s?.Trim().ToLowerInvariant()))
            .WithMessage("Status must be either 'resolved' or 'waived_with_exception'.");

        RuleFor(x => x.ResolutionNotes)
            .MaximumLength(4000)
            .When(x => !string.IsNullOrWhiteSpace(x.ResolutionNotes))
            .WithMessage("Resolution notes must not exceed 4000 characters.");

        RuleFor(x => x.ResolutionAction)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.ResolutionAction))
            .WithMessage("Resolution action must not exceed 50 characters.");
    }
}
