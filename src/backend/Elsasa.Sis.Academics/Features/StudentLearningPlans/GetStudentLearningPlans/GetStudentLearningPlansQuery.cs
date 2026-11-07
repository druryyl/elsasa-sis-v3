namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;

/// <summary>
/// Query parameters for paginated and filterable student learning plan roster (SCR-ACD-04, P2-S06).
/// </summary>
/// <param name="AcademicPeriodId">Optional operational academic period filter.</param>
/// <param name="Status">Optional plan status filter ('draft', 'active', 'revised', 'completed', 'archived').</param>
/// <param name="StudentSearch">Optional search query matching student name or NIS.</param>
/// <param name="Page">Page number (1-based, default 1).</param>
/// <param name="PageSize">Page size (default 20, max 100).</param>
public sealed record GetStudentLearningPlansQuery(
    Guid? AcademicPeriodId = null,
    string? Status = null,
    string? StudentSearch = null,
    int Page = 1,
    int PageSize = 20);
