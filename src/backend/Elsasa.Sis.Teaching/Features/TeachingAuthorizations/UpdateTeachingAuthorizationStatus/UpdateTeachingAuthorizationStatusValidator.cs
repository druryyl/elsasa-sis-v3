namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.UpdateTeachingAuthorizationStatus;

using FluentValidation;

/// <summary>
/// Validator for UpdateTeachingAuthorizationStatusRequest (TD-05, P4-S12).
/// Validates status is in ('active', 'authorized_under_supervision', 'restricted', 'suspended', 'revoked', 'expired') and mandates status_reason.
/// </summary>
public sealed class UpdateTeachingAuthorizationStatusValidator : AbstractValidator<UpdateTeachingAuthorizationStatusRequest>
{
    private static readonly HashSet<string> _allowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active",
        "authorized_under_supervision",
        "restricted",
        "suspended",
        "revoked",
        "expired"
    };

    public UpdateTeachingAuthorizationStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => !string.IsNullOrWhiteSpace(s) && _allowedStatuses.Contains(s.Trim()))
            .WithMessage("Status must be one of: 'active', 'authorized_under_supervision', 'restricted', 'suspended', 'revoked', 'expired'.");

        RuleFor(x => x.StatusReason)
            .Must((request, _) => !string.IsNullOrWhiteSpace(request.ResolvedStatusReason))
            .WithMessage("Status justification reason is mandatory.")
            .DependentRules(() =>
            {
                RuleFor(x => x.ResolvedStatusReason)
                    .MaximumLength(2000)
                    .WithMessage("Status reason must not exceed 2000 characters.");
            });
    }
}
