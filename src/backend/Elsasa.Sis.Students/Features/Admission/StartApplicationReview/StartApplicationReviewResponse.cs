namespace Elsasa.Sis.Students.Features.Admission.StartApplicationReview;

/// <summary>
/// Response payload returned after transitioning an application to under review (ADR-005).
/// </summary>
public sealed record StartApplicationReviewResponse(
    Guid Id,
    string Status,
    DateTimeOffset? UpdatedAt = null);
