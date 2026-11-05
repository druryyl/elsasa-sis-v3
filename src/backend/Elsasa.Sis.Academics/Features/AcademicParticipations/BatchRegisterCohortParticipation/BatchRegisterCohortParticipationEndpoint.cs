namespace Elsasa.Sis.Academics.Features.AcademicParticipations.BatchRegisterCohortParticipation;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for batch registering cohort academic participations (P2-S04, GAP-003, UC-ACD-10).
/// </summary>
public sealed class BatchRegisterCohortParticipationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/participations/batch-cohort", HandleAsync)
            .WithName("BatchRegisterCohortParticipation")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Participations.Manage)
            .WithValidation<BatchRegisterCohortParticipationRequest>()
            .Produces<BatchRegisterCohortParticipationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        BatchRegisterCohortParticipationRequest request,
        BatchRegisterCohortParticipationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
