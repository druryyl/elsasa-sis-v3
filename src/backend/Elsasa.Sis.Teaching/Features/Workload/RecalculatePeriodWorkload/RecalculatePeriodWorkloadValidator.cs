namespace Elsasa.Sis.Teaching.Features.Workload.RecalculatePeriodWorkload;

using FluentValidation;

/// <summary>
/// FluentValidation validator for <see cref="RecalculatePeriodWorkloadRequest"/> (TD-04, P3-S09).
/// </summary>
public sealed class RecalculatePeriodWorkloadValidator : AbstractValidator<RecalculatePeriodWorkloadRequest>
{
    public RecalculatePeriodWorkloadValidator()
    {
        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty()
            .WithMessage("Academic period identifier is required.");
    }
}
