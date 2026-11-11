namespace Elsasa.Sis.Teaching.Features.AssignmentCompletions.ConcludeTeachingAssignment;

using FluentValidation;

/// <summary>
/// Validator for <see cref="ConcludeTeachingAssignmentRequest"/> (TD-01, TD-04, P4-S13).
/// </summary>
public sealed class ConcludeTeachingAssignmentValidator : AbstractValidator<ConcludeTeachingAssignmentRequest>
{
    private static readonly string[] _validUnresolvedStatuses =
    [
        "none",
        "resolved",
        "escalated",
        "minor_pending",
        "escalated_defect"
    ];

    public ConcludeTeachingAssignmentValidator()
    {
        RuleFor(x => x.CompletionDate)
            .NotEmpty()
            .WithMessage("Completion date is required.");

        RuleFor(x => x.AcademicConfirmationSource)
            .MaximumLength(256)
            .WithMessage("Academic confirmation source must not exceed 256 characters.");

        RuleFor(x => x.UnresolvedItemsStatus)
            .Must(status => string.IsNullOrWhiteSpace(status) || _validUnresolvedStatuses.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage("Unresolved items status must be one of: 'none', 'minor_pending', 'escalated_defect', 'resolved', 'escalated'.");

        RuleFor(x => x.UnresolvedItemsDescription)
            .MaximumLength(2000)
            .WithMessage("Unresolved items description must not exceed 2000 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes must not exceed 2000 characters.");
    }
}
