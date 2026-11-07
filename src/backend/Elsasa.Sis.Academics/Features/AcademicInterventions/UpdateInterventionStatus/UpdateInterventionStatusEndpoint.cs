namespace Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;

using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for updating academic intervention status (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class UpdateInterventionStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPatch("/api/v1/academics/interventions/{id:guid}/status", HandleAsync)
            .WithName("UpdateInterventionStatus")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Interventions.UpdateStatus)
            .WithValidation<UpdateInterventionStatusRequest>()
            .Produces<AcademicInterventionDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateInterventionStatusRequest request,
        UpdateInterventionStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateInterventionStatusCommand(id, request.Status, request.OutcomeNotes);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
