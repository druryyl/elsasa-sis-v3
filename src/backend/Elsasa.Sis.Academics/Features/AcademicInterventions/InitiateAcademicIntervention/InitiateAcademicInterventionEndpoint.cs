namespace Elsasa.Sis.Academics.Features.AcademicInterventions.InitiateAcademicIntervention;

using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for initiating an academic intervention (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class InitiateAcademicInterventionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/interventions", HandleAsync)
            .WithName("InitiateAcademicIntervention")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Interventions.Initiate)
            .WithValidation<InitiateAcademicInterventionCommand>()
            .Produces<AcademicInterventionDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        InitiateAcademicInterventionCommand command,
        InitiateAcademicInterventionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/interventions/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
