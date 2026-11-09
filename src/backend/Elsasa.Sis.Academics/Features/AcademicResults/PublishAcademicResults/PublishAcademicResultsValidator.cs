namespace Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;

using FluentValidation;

/// <summary>
/// Validator for <see cref="PublishAcademicResultsRequest"/> (P4-S09).
/// </summary>
public sealed class PublishAcademicResultsValidator : AbstractValidator<PublishAcademicResultsRequest>
{
    public PublishAcademicResultsValidator()
    {
        RuleForEach(x => x.ResultIds)
            .NotEmpty().WithMessage("Result ID cannot be empty.")
            .When(x => x.ResultIds != null);
    }
}
