namespace Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation;

using FluentValidation;

/// <summary>
/// Validator for participation conclusion command (P2-S04, UC-ACD-12).
/// </summary>
public sealed class ConcludeParticipationValidator : AbstractValidator<ConcludeParticipationCommand>
{
    public ConcludeParticipationValidator()
    {
        RuleFor(x => x.ParticipationId)
            .NotEmpty().WithMessage("Participation ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
