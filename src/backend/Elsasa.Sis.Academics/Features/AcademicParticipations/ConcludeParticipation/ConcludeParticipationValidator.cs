namespace Elsasa.Sis.Academics.Features.AcademicParticipations.ConcludeParticipation;

using FluentValidation;

/// <summary>
/// Validator for conclude participation requests (P2-S05, UC-ACD-12).
/// </summary>
public sealed class ConcludeParticipationValidator : AbstractValidator<ConcludeParticipationRequest>
{
    public ConcludeParticipationValidator()
    {
        RuleFor(x => x.CompletionDate)
            .NotEmpty().WithMessage("Completion date is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
