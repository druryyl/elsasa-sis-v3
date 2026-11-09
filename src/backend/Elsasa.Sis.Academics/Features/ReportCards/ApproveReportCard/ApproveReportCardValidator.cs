namespace Elsasa.Sis.Academics.Features.ReportCards.ApproveReportCard;

using FluentValidation;

/// <summary>
/// Validator for report card certification request.
/// </summary>
public sealed class ApproveReportCardValidator : AbstractValidator<ApproveReportCardRequest>
{
    public ApproveReportCardValidator()
    {
        RuleFor(x => x.NarrativeComments)
            .MaximumLength(4000)
            .WithMessage("Narrative comments must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.NarrativeComments));
    }
}
