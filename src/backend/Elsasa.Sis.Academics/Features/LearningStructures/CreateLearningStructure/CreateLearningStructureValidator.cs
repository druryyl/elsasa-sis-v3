namespace Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;

using FluentValidation;

/// <summary>
/// Validates <see cref="CreateLearningStructureRequest"/> fields before execution (ADR-005, P3-S06).
/// </summary>
public sealed class CreateLearningStructureValidator : AbstractValidator<CreateLearningStructureRequest>
{
    public CreateLearningStructureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required.")
            .MaximumLength(50).WithMessage("Version cannot exceed 50 characters.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.EffectiveDate)
            .WithMessage("End date must be greater than or equal to effective date.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
