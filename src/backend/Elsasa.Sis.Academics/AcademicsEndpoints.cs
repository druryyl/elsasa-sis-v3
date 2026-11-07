namespace Elsasa.Sis.Academics;

using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API route registration for the Academics domain (ADR-004, ADR-005, P3-S10, P5-S13, P6-S15).
/// Maps endpoints for:
/// - Learning Structures, Units &amp; Requirements (/api/v1/academics/learning-structures/*, learning-units/*, academic-requirements/*)
/// - Academic Participations (/api/v1/academics/participations/*)
/// - Learning Activities (/api/v1/academics/learning-activities/*, /api/v1/academics/teacher/activities)
/// - Student Attendance &amp; Corrections (/api/v1/academics/learning-activities/{id}/attendance, /api/v1/academics/attendance-records/*, /api/v1/academics/students/{id}/attendance-summary)
/// - Coursework Assignments &amp; Submissions (/api/v1/academics/assignments/*, /api/v1/academics/submissions/*)
/// - Assessment Planning &amp; Evidence (/api/v1/academics/assessments/*)
/// - Academic Results Evaluation, Moderation &amp; Publication (/api/v1/academics/assessments/{id}/results/*)
/// - Auditable Record Corrections (/api/v1/academics/academic-results/{id}/correct, /academic-standings/{id}/correct, /promotion-recommendations/{id}/correct, /corrections)
/// - Report Cards Compilation, Approval &amp; Inquiries (/api/v1/academics/report-cards/*)
/// - Interim Transcripts Generation &amp; Issuance (/api/v1/academics/transcripts/*)
/// - Learning Progress &amp; Participation Conclusion (/api/v1/academics/learning-progress/*, /learning-units/{unitId}/progress, /students/{studentId}/learning-progress)
/// - Academic Standings Single &amp; Batch Determinations (/api/v1/academics/academic-standings/*, /students/{studentId}/standings)
/// - Promotion Recommendations Single &amp; Batch Issuance (/api/v1/academics/promotion-recommendations/*, /students/{studentId}/promotion-recommendations)
/// - Academic Interventions &amp; At-Risk Query Engine (/api/v1/academics/interventions/*)
/// - Academic Achievements Conferral &amp; Inquiries (/api/v1/academics/achievements, /api/v1/academics/students/{studentId}/achievements, /api/v1/academics/cohort-review/achievements)
/// - Cohort Academic Review &amp; Dynamic Evaluations (/api/v1/academics/cohort-review/*)
/// - Student Learning Self-Service Workspace (/api/v1/portal/learning/*)
/// </summary>
public static class AcademicsEndpoints
{
    /// <summary>
    /// Maps all Academics domain endpoints across Submodules 1, 2, 3, and 4 (Standing &amp; Progression).
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The route builder for fluent chaining.</returns>
    public static IEndpointRouteBuilder MapAcademicsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Discover and map all IEndpoint implementations in the Academics assembly
        app.MapEndpoints(typeof(AcademicsAssemblyMarker).Assembly);

        return app;
    }
}
