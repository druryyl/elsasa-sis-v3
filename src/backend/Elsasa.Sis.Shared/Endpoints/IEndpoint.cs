namespace Elsasa.Sis.Shared.Endpoints;

using Microsoft.AspNetCore.Routing;

/// <summary>
/// Defines an HTTP endpoint contract for Minimal API vertical slices (ADR-005).
/// Implementors are discovered and registered automatically at application startup.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Configures route mapping and metadata for this endpoint on the route builder.
    /// </summary>
    /// <param name="app">The route builder on which to map endpoints.</param>
    void MapEndpoint(IEndpointRouteBuilder app);
}
