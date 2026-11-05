namespace Elsasa.Sis.Academics.Features.AcademicParticipations.UpdateParticipationStatus;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for updating participation status (P2-S05, GAP-08, UC-ACD-11).
/// </summary>
public sealed class UpdateParticipationStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/v1/academics/participations/{id:guid}/status", HandleAsync)
            .WithName("UpdateParticipationStatus")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Participations.Manage)
            .WithValidation<UpdateParticipationStatusRequest>()
            .Produces<UpdateParticipationStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateParticipationStatusRequest request,
        UpdateParticipationStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
