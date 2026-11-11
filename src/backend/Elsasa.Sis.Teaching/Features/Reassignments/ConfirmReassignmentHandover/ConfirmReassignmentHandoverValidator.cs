namespace Elsasa.Sis.Teaching.Features.Reassignments.ConfirmReassignmentHandover;

using FluentValidation;

/// <summary>
/// Validator for confirming reassignment handover.
/// </summary>
public sealed class ConfirmReassignmentHandoverValidator : AbstractValidator<ConfirmReassignmentHandoverRequest>
{
    private static readonly string[] _allowedStatuses = ["completed", "waived"];

    public ConfirmReassignmentHandoverValidator()
    {
        RuleFor(x => x.HandoverStatus)
            .Must(status => string.IsNullOrWhiteSpace(status) || _allowedStatuses.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage("Handover status must be either 'completed' or 'waived'.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
