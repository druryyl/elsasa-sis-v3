namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.ResolveSupervisionRequirement;

using FluentValidation;

/// <summary>
/// FluentValidation rules for <see cref="ResolveSupervisionRequirementRequest"/> (P4-S13).
/// </summary>
public sealed class ResolveSupervisionRequirementValidator : AbstractValidator<ResolveSupervisionRequirementRequest>
{
    private static readonly HashSet<string> _allowedStatuses =
    [
        "completed",
        "waived",
        "escalated"
    ];

    public ResolveSupervisionRequirementValidator()
    {
        RuleFor(x => x.EffectiveStatus)
            .NotEmpty().WithMessage("Resolution status is required.")
            .Must(status => !string.IsNullOrWhiteSpace(status) && _allowedStatuses.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage("Resolution status must be one of: completed, waived, escalated.");

        RuleFor(x => x.ResolutionNotes)
            .NotEmpty().WithMessage("Resolution notes are mandatory.")
            .MaximumLength(4000).WithMessage("Resolution notes cannot exceed 4000 characters.");
    }
}
