namespace Elsasa.Sis.Teaching;

using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API route registration for the Teaching domain (ADR-004, ADR-005, P1-S03).
/// Maps endpoint group /api/v1/teaching and registers all IEndpoint instances in the Teaching assembly.
/// </summary>
public static class TeachingEndpoints
{
    /// <summary>
    /// Maps all Teaching domain endpoints under /api/v1/teaching.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The route builder for fluent chaining.</returns>
    public static IEndpointRouteBuilder MapTeachingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Map route group for /api/v1/teaching
        _ = app.MapGroup("/api/v1/teaching")
            .WithTags("Teaching");

        // Discover and map all IEndpoint implementations in the Teaching assembly
        app.MapEndpoints(typeof(TeachingAssemblyMarker).Assembly);

        return app;
    }
}
