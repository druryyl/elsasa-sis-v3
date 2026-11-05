namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RecognizePriorCredit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for recognizing transfer or prior learning credit (P2-S05, GAP-08, UC-ACD-11).
/// </summary>
public sealed class RecognizePriorCreditEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/participations/recognize-prior", HandleAsync)
            .WithName("RecognizePriorCredit")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Participations.RecognizePrior)
            .WithValidation<RecognizePriorCreditRequest>()
            .Produces<RecognizePriorCreditResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        RecognizePriorCreditRequest request,
        RecognizePriorCreditHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/participations/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
