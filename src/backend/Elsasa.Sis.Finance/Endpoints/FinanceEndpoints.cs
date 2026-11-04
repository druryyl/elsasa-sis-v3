namespace Elsasa.Sis.Finance.Endpoints;

using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API route group definition and endpoint wiring for the Finance domain (ADR-004, ADR-009).
/// </summary>
public sealed class FinanceEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Map root /api/finance route group descriptor
        app.MapGroup("/api/finance")
            .WithTags("Finance")
            .MapGet("/", () => Results.Ok(new FinanceDomainStatus(
                Domain: "Finance",
                Status: "active",
                Version: "1.0",
                RoutePrefix: "/api/finance")))
            .WithName("GetFinanceDomainStatus")
            .RequireAuthorization();
    }
}

/// <summary>
/// Status descriptor for the Finance domain route group.
/// </summary>
public sealed record FinanceDomainStatus(string Domain, string Status, string Version, string RoutePrefix);
