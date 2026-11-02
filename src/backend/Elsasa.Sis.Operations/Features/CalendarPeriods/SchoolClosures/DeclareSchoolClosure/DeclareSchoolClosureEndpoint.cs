namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.DeclareSchoolClosure;

using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for declaring a school closure (ADR-005, UC-OPS-12).
/// </summary>
public sealed class DeclareSchoolClosureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/closures", HandleAsync)
            .WithName("DeclareSchoolClosure")
            .WithTags("SchoolClosures")
            .RequireAuthorization(Permissions.Operations.Closure.Declare)
            .WithValidation<DeclareSchoolClosureRequest>()
            .Produces<SchoolClosureResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        DeclareSchoolClosureRequest request,
        DeclareSchoolClosureHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/operations/closures/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
