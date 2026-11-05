namespace Elsasa.Sis.Academics.Features.AcademicParticipations.UpdateParticipationStatus;

using FluentValidation;

/// <summary>
/// Validator for academic participation status transition requests (P2-S05).
/// </summary>
public sealed class UpdateParticipationStatusValidator : AbstractValidator<UpdateParticipationStatusRequest>
{
    private static readonly string[] _allowedStatuses = ["in_progress", "paused", "withdrawn"];

    public UpdateParticipationStatusValidator()
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty().WithMessage("New status is required.")
            .Must(status => status != null && _allowedStatuses.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage("New status must be one of: in_progress, paused, withdrawn.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
