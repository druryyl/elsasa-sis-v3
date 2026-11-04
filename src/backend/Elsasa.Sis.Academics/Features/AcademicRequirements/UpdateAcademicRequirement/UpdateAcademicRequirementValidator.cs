namespace Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;

using FluentValidation;

/// <summary>
/// Validator for <see cref="UpdateAcademicRequirementRequest"/> (ADR-005, P3-S09).
/// </summary>
public sealed class UpdateAcademicRequirementValidator : AbstractValidator<UpdateAcademicRequirementRequest>
{
    private static readonly string[] _validScopes = ["program", "level", "learning_unit"];
    private static readonly string[] _validTypes = ["completion", "prerequisite", "credit", "progression", "graduation", "assessment_policy"];

    public UpdateAcademicRequirementValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.RequirementScope)
            .Must(scope => scope is null || _validScopes.Contains(scope.Trim().ToLowerInvariant()))
            .WithMessage("Requirement scope must be 'program', 'level', or 'learning_unit'.")
            .When(x => !string.IsNullOrWhiteSpace(x.RequirementScope));

        RuleFor(x => x.RequirementType)
            .Must(type => type is null || _validTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage("Requirement type must be 'completion', 'prerequisite', 'credit', 'progression', 'graduation', or 'assessment_policy'.")
            .When(x => !string.IsNullOrWhiteSpace(x.RequirementType));

        RuleFor(x => x.Version)
            .MaximumLength(50).WithMessage("Version cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Version));

        RuleFor(x => x.Level)
            .MaximumLength(50).WithMessage("Level cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Level));

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.EffectiveDate!.Value)
            .WithMessage("End date must be greater than or equal to effective date.")
            .When(x => x.EndDate.HasValue && x.EffectiveDate.HasValue);
    }
}
