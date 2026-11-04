namespace Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

/// <summary>
/// Candidate student evaluation item within a batch assessment billing cycle.
/// </summary>
/// <param name="StudentId">Unique identifier of the candidate student.</param>
/// <param name="StudentNumber">Human-readable student admission/enrollment number.</param>
/// <param name="StudentFullName">Full name of the candidate student.</param>
/// <param name="Campus">Active placement campus.</param>
/// <param name="Grade">Active placement grade / educational level.</param>
/// <param name="AccountId">Student financial account identifier if provisioned.</param>
/// <param name="AccountNumber">Student financial account number if provisioned.</param>
/// <param name="BaseAmount">The base monetary amount to be assessed in IDR integer representation.</param>
/// <param name="DueDate">The computed calendar due date derived from rule parameters.</param>
/// <param name="IsDuplicate">Indicates if this student already has an assessed obligation for this rule and period.</param>
/// <param name="Notes">Optional contextual notes or duplicate rationale.</param>
public sealed record BatchAssessCandidateDto(
    Guid StudentId,
    string StudentNumber,
    string StudentFullName,
    string Campus,
    string Grade,
    Guid? AccountId,
    string? AccountNumber,
    long BaseAmount,
    DateOnly DueDate,
    bool IsDuplicate,
    string? Notes = null);
