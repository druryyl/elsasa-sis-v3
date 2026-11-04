namespace Elsasa.Sis.Academics;

using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API route registration for the Academics domain (ADR-004, ADR-005, P3-S10).
/// </summary>
public static class AcademicsEndpoints
{
    /// <summary>
    /// Maps all structure, unit, and requirement endpoints under /api/v1/academics/ with tag "Academics".
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
