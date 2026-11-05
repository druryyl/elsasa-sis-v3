namespace Elsasa.Sis.Academics.Features.Assignments.CreateAssignment;

using FluentValidation;

/// <summary>
/// Validator for <see cref="CreateAssignmentRequest"/> (P5-S10, TD-05).
/// </summary>
public sealed class CreateAssignmentValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ReleaseDate)
            .NotEmpty().WithMessage("Release date is required.");

        RuleFor(x => x.DueDate)
            .Must((request, dueDate) => !dueDate.HasValue || dueDate.Value >= request.ReleaseDate)
            .WithMessage("Due date must be on or after the release date.")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
