namespace Elsasa.Sis.Academics;

using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API route registration for the Academics domain (ADR-004, ADR-005, P3-S10, P5-S13).
/// Maps endpoints for:
/// - Learning Structures, Units &amp; Requirements (/api/v1/academics/learning-structures/*, learning-units/*, academic-requirements/*)
/// - Academic Participations (/api/v1/academics/participations/*)
/// - Learning Activities (/api/v1/academics/learning-activities/*, /api/v1/academics/teacher/activities)
/// - Student Attendance &amp; Corrections (/api/v1/academics/learning-activities/{id}/attendance, /api/v1/academics/attendance-records/*, /api/v1/academics/students/{id}/attendance-summary)
/// - Coursework Assignments &amp; Submissions (/api/v1/academics/assignments/*, /api/v1/academics/submissions/*)
/// - Student Coursework Portal (/api/v1/portal/learning/*)
/// </summary>
public static class AcademicsEndpoints
{
    /// <summary>
    /// Maps all Academics domain endpoints across Submodule 1 and Submodule 2.
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
