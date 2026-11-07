namespace Elsasa.Sis.Api.Features.DemoData;

using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>
/// Request payload for resetting demo data (TD-07, TD-08).
/// </summary>
/// <param name="CleanOnly">If true, only purges all schemas without seeding new data.</param>
public sealed record ResetDemoDataRequest(bool CleanOnly = false);

/// <summary>
/// Minimal API endpoint for resetting demo data (ADR-006, OQ-004, TD-07, TD-08).
/// Secured by SuperAdmin role and guarded by production environment check.
/// </summary>
public sealed class DemoDataEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/demo-data/reset", HandleAsync)
            .WithName("DemoDataReset")
            .WithTags("DemoData")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" })
            .Produces<DemoResetSummary>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        ResetDemoDataRequest? request,
        DemoDataOrchestrator orchestrator,
        IHostEnvironment environment,
        IOptions<DemoDataOptions> optionsAccessor,
        CancellationToken cancellationToken)
    {
        // Production safeguard (ADR-006, OQ-004, TD-08)
        if (environment.IsProduction())
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Action Forbidden",
                detail: "Demo data generation and reset is strictly prohibited in Production environments.");
        }

        // Configuration check (ADR-006)
        var options = optionsAccessor.Value;
        if (!options.Enabled)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Demo Data Disabled",
                detail: "Demo data operations are disabled via configuration (DemoData:Enabled = false).");
        }

        var isCleanOnly = request?.CleanOnly ?? false;
        var summary = isCleanOnly
            ? await orchestrator.ExecuteResetAsync(cleanOnly: true, cancellationToken: cancellationToken)
            : await orchestrator.ResetAndSeedAsync(cancellationToken);

        return Results.Ok(summary);
    }
}
