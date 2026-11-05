namespace Elsasa.Sis.Academics.Features.AcademicParticipations.BatchRegisterCohortParticipation;

using FluentValidation;

/// <summary>
/// Validator for cohort batch academic participation registration requests (P2-S04).
/// </summary>
public sealed class BatchRegisterCohortParticipationValidator : AbstractValidator<BatchRegisterCohortParticipationRequest>
{
    public BatchRegisterCohortParticipationValidator()
    {
        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.AcademicPeriodLabel)
            .NotEmpty().WithMessage("Academic period label is required.")
            .MaximumLength(100).WithMessage("Academic period label cannot exceed 100 characters.");

        RuleFor(x => x.LearningUnitIds)
            .NotEmpty().WithMessage("At least one learning unit ID must be specified.");

        RuleForEach(x => x.LearningUnitIds)
            .NotEmpty().WithMessage("Learning unit ID must not be empty.");

        RuleFor(x => x.SectionLabel)
            .NotEmpty().WithMessage("Section label is required.")
            .MaximumLength(50).WithMessage("Section label cannot exceed 50 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        When(x => x.StudentIds is not null, () =>
        {
            RuleForEach(x => x.StudentIds)
                .NotEmpty().WithMessage("Student ID must not be empty.");
        });
    }
}
