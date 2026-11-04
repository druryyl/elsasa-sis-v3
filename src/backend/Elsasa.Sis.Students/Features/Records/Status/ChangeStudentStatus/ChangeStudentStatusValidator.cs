namespace Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;

using FluentValidation;

/// <summary>
/// Validator enforcing business invariants and input requirements for <see cref="ChangeStudentStatusRequest"/> (GAP-005, TD-05).
/// </summary>
public sealed class ChangeStudentStatusValidator : AbstractValidator<ChangeStudentStatusRequest>
{
    private static readonly string[] _allowedStatuses = ["active", "enrolled"];

    public ChangeStudentStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(status => status != null && _allowedStatuses.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage($"Status must be one of: {string.Join(", ", _allowedStatuses)}.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty()
            .WithMessage("Effective date is required.")
            .NotEqual(default(DateOnly))
            .WithMessage("Effective date must be a valid date.");

        RuleFor(x => x.AuthorizedBy)
            .MaximumLength(100)
            .WithMessage("AuthorizedBy cannot exceed 100 characters.");

        RuleFor(x => x.DecisionReference)
            .MaximumLength(100)
            .WithMessage("DecisionReference cannot exceed 100 characters.");
    }
}
