namespace Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;

using FluentValidation;

/// <summary>
/// Validator for <see cref="ApproveSummativeResultsRequest"/> (P4-S09).
/// </summary>
public sealed class ApproveSummativeResultsValidator : AbstractValidator<ApproveSummativeResultsRequest>
{
    public ApproveSummativeResultsValidator()
    {
        RuleForEach(x => x.ResultIds)
            .NotEmpty().WithMessage("Result ID cannot be empty.")
            .When(x => x.ResultIds != null);
    }
}
