namespace Elsasa.Sis.Academics.Features.StudentLearningPlans;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Route group configuration and metadata constants for Student Learning Plans Minimal API endpoints (SCR-ACD-04, UC-ACD-09, P4-S11).
/// Groups all 14 Minimal API endpoints under <c>/api/v1/academics/learning-plans</c> with <c>.WithTags("StudentLearningPlans")</c>.
/// </summary>
public static class StudentLearningPlansEndpoints
{
    public const string BaseRoute = "/api/v1/academics/learning-plans";
    public const string EndpointTag = "StudentLearningPlans";

    /// <summary>
    /// Configures the route group for Student Learning Plans endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>A configured <see cref="RouteGroupBuilder"/>.</returns>
    public static RouteGroupBuilder MapStudentLearningPlansEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.MapGroup(BaseRoute)
            .WithTags(EndpointTag);
    }
}
