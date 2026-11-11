namespace Elsasa.Sis.Teaching.Features.Reassignments.ProposePermanentReassignment;

using FluentValidation;

/// <summary>
/// FluentValidation rules for proposing a permanent teaching reassignment.
/// </summary>
public sealed class ProposePermanentReassignmentValidator : AbstractValidator<ProposePermanentReassignmentRequest>
{
    public ProposePermanentReassignmentValidator()
    {
        RuleFor(x => x.OriginalAssignmentId)
            .NotEmpty()
            .WithMessage("Original assignment ID is required.");

        RuleFor(x => x.NewTeacherId)
            .NotEmpty()
            .WithMessage("New teacher profile ID is required.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty()
            .WithMessage("Effective date is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason for reassignment is required.")
            .MaximumLength(2000)
            .WithMessage("Reason must not exceed 2000 characters.");

        RuleFor(x => x.ReassignmentType)
            .MaximumLength(50)
            .WithMessage("Reassignment type must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReassignmentType));
    }
}
