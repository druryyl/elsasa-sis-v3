namespace Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;

/// <summary>
/// Domain logic evaluator for coursework submission lineage and timeliness rules (TD-05, TD-06, UC-ACD-01).
/// </summary>
public static class CourseworkSubmissionEvaluator
{
    /// <summary>
    /// Determines whether a submission is a resubmission and establishes the predecessor link.
    /// </summary>
    /// <param name="priorSubmissionId">The ID of the latest existing submission for the student on this assignment, if any.</param>
    /// <returns>A tuple of (<c>IsResubmission</c>, <c>PreviousSubmissionId</c>).</returns>
    public static (bool IsResubmission, Guid? PreviousSubmissionId) DetermineLineage(Guid? priorSubmissionId)
    {
        return (priorSubmissionId.HasValue, priorSubmissionId);
    }

    /// <summary>
    /// Evaluates submission timeliness against the assignment due date.
    /// </summary>
    /// <param name="dueDate">Optional assignment deadline date.</param>
    /// <param name="today">Evaluation date (typically current UTC date).</param>
    /// <returns><c>"late"</c> if past due date; otherwise <c>"submitted"</c>.</returns>
    public static string EvaluateTimeliness(DateOnly? dueDate, DateOnly today)
    {
        return (dueDate.HasValue && today > dueDate.Value) ? "late" : "submitted";
    }
}
