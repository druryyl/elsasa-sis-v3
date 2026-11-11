namespace Elsasa.Sis.Teaching.Features.TeacherEligibility.EvaluateCandidateEligibility;

using FluentValidation;

/// <summary>
/// Validator for candidate teacher eligibility evaluation request (TD-03, P2-S05).
/// </summary>
public sealed class EvaluateCandidateEligibilityValidator : AbstractValidator<EvaluateCandidateEligibilityRequest>
{
    public EvaluateCandidateEligibilityValidator()
    {
        RuleFor(x => x.TeachingDemandId)
            .NotEmpty()
            .WithMessage("Teaching demand ID is required.");

        RuleFor(x => x.TeacherProfileId)
            .NotEmpty()
            .WithMessage("Teacher profile ID is required.");

        RuleFor(x => x.CoordinatorRemarks)
            .MaximumLength(2000)
            .WithMessage("Coordinator remarks must not exceed 2000 characters.");
    }
}
