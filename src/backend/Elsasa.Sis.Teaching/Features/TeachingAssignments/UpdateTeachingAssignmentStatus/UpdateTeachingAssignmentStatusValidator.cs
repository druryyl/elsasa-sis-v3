namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.UpdateTeachingAssignmentStatus;

using FluentValidation;

/// <summary>
/// Validator for updating teaching assignment status (TD-01, TD-04, P3-S06).
/// </summary>
public sealed class UpdateTeachingAssignmentStatusValidator : AbstractValidator<UpdateTeachingAssignmentStatusRequest>
{
    private static readonly string[] _allowedStatuses =
    [
        "suspended",
        "cancelled",
        "withdrawn",
        "returned_for_revision"
    ];

    public UpdateTeachingAssignmentStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(s => _allowedStatuses.Contains(s?.Trim().ToLowerInvariant()))
            .WithMessage("Status must be one of: suspended, cancelled, withdrawn, returned_for_revision.");

        RuleFor(x => x.Reason)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Reason))
            .WithMessage("Reason must not exceed 2000 characters.");
    }
}
