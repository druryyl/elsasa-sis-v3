namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemandStatus;

using FluentValidation;

/// <summary>
/// FluentValidation validator enforcing demand lifecycle transition rules (TD-02, P2-S04).
/// </summary>
public sealed class UpdateTeachingDemandStatusValidator : AbstractValidator<UpdateTeachingDemandStatusRequest>
{
    private static readonly string[] _allowedStatuses =
    [
        "accepted_for_staffing",
        "returned_for_clarification",
        "declined",
        "cancelled"
    ];

    public UpdateTeachingDemandStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => _allowedStatuses.Contains(s?.Trim().ToLowerInvariant()))
            .WithMessage($"Invalid target status. Allowed transitions: {string.Join(", ", _allowedStatuses)}.");

        RuleFor(x => x.DeclineReason)
            .NotEmpty()
            .WithMessage("Decline reason is mandatory when declining a teaching demand.")
            .When(x => string.Equals(x.Status?.Trim(), "declined", StringComparison.OrdinalIgnoreCase));
    }
}
