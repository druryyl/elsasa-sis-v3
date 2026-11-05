namespace Elsasa.Sis.Academics.Features.AcademicParticipations.ConcludeParticipation;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for concluding academic participation (P2-S05, GAP-08, UC-ACD-12).
/// </summary>
public sealed class ConcludeParticipationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/participations/{id:guid}/conclude", HandleAsync)
            .WithName("ConcludeParticipation")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Participations.Conclude)
            .WithValidation<ConcludeParticipationRequest>()
            .Produces<ConcludeParticipationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ConcludeParticipationRequest request,
        ConcludeParticipationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
