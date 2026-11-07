namespace Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortProgression;

using Elsasa.Sis.Academics.Features.CohortReview;
using Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;
using Elsasa.Sis.Shared.Common;
using FluentValidation;

/// <summary>
/// Feature handler dynamically evaluating proposed cohort grade progression recommendations (TD-03, UC-ACD-17, P3-S06).
/// Consumes committed or proposed academic standing determinations, computes progression classifications,
/// recommended destination grade levels, pre-populated condition requirements, and supporting evidence summaries.
/// </summary>
public sealed class EvaluateCohortProgressionHandler(
    EvaluateCohortStandingHandler standingHandler,
    ThresholdRuleEvaluator thresholdEvaluator,
    IValidator<EvaluateCohortProgressionRequest>? validator = null)
{
    public async Task<Result<IReadOnlyList<CohortProgressionProposalDto>>> HandleAsync(
        EvaluateCohortProgressionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<IReadOnlyList<CohortProgressionProposalDto>>(
                    DomainError.Validation("CohortProgression.Validation", validationResult.ToString()));
            }
        }

        // 1. Evaluate cohort standings to gather student metrics, completed units, and standing states
        var standingResult = await standingHandler.HandleAsync(
            new EvaluateCohortStandingRequest(request.AcademicPeriodId, request.GradeLevel),
            cancellationToken);

        if (standingResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<CohortProgressionProposalDto>>(standingResult.Error);
        }

        var standingProposals = standingResult.Value;
        var progressionProposals = new List<CohortProgressionProposalDto>(standingProposals.Count);

        foreach (var proposal in standingProposals)
        {
            // Prefer authoritative saved standing if committed; otherwise use dynamically proposed standing
            var isCommitted = !string.IsNullOrWhiteSpace(proposal.ExistingStanding);
            var activeStanding = isCommitted ? proposal.ExistingStanding! : proposal.ProposedStanding;
            var failingUnits = proposal.FailingUnitNames ?? Array.Empty<string>();

            var (recommendation, recommendedToLevel, conditions, evidence) = thresholdEvaluator.EvaluateProgression(
                standing: activeStanding,
                fromLevel: proposal.GradeLevel,
                totalUnits: proposal.TotalUnits,
                completedUnits: proposal.CompletedUnits,
                failedUnitNames: failingUnits,
                attendanceRate: proposal.AttendanceRate,
                conditions: proposal.Conditions);

            progressionProposals.Add(new CohortProgressionProposalDto(
                StudentId: proposal.StudentId,
                StudentNumber: proposal.StudentNumber,
                StudentName: proposal.StudentName,
                FromLevel: proposal.GradeLevel,
                RecommendedToLevel: recommendedToLevel,
                ProposedRecommendation: recommendation,
                Conditions: conditions,
                SupportingEvidence: evidence,
                CurrentStanding: activeStanding,
                IsStandingCommitted: isCommitted));
        }

        return Result.Success<IReadOnlyList<CohortProgressionProposalDto>>(progressionProposals);
    }
}
