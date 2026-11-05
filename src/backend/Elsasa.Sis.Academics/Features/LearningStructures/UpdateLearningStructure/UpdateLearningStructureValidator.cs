namespace Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;

using FluentValidation;

/// <summary>
/// Validates <see cref="UpdateLearningStructureRequest"/> fields before update execution (ADR-005, P3-S06).
/// </summary>
public sealed class UpdateLearningStructureValidator : AbstractValidator<UpdateLearningStructureRequest>
{
    public UpdateLearningStructureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
