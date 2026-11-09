namespace Elsasa.Sis.Academics.Features.AcademicInterventions.GetActiveInterventions;

using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying active and historical academic interventions (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class GetActiveInterventionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/interventions", HandleAsync)
            .WithName("GetActiveInterventions")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Interventions.View)
            .Produces<IReadOnlyList<AcademicInterventionDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] string? status,
        [FromQuery] Guid? studentId,
        GetActiveInterventionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicPeriodId, status, studentId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
