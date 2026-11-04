namespace Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;

/// <summary>
/// Command payload for assessing an individual student obligation (FIN-WF-02, UC-FIN-02, GAP-003, TD-04).
/// </summary>
/// <param name="StudentId">The unique identifier of the target student.</param>
/// <param name="ChargeType">The category/type of the ad-hoc charge (e.g. library_fee, activity_fee, uniform, replacement).</param>
/// <param name="Description">Descriptive narrative of the fee or obligation item.</param>
/// <param name="Amount">The assessed integer monetary amount in IDR (BIGINT > 0).</param>
/// <param name="DueDate">The settlement due date.</param>
/// <param name="AcademicPeriodId">The associated operational academic period identifier.</param>
/// <param name="Notes">Optional administrative or contextual remarks.</param>
/// <param name="PolicyId">Optional governing policy UUID. When omitted, resolves active tuition/institutional policy.</param>
public sealed record AssessAdHocObligationRequest(
    Guid StudentId,
    string ChargeType,
    string Description,
    long Amount,
    DateOnly DueDate,
    Guid AcademicPeriodId,
    string? Notes = null,
    Guid? PolicyId = null);
