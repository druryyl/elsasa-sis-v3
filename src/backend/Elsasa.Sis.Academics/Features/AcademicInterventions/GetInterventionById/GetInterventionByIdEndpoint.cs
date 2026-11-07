namespace Elsasa.Sis.Academics.Features.AcademicInterventions.GetInterventionById;

using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for retrieving a single academic intervention by ID (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class GetInterventionByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/interventions/{id:guid}", HandleAsync)
            .WithName("GetInterventionById")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Interventions.View)
            .Produces<AcademicInterventionDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetInterventionByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
