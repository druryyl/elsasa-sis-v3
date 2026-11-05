namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RegisterParticipation;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for registering a single academic participation (P2-S04, TD-02, UC-ACD-10).
/// </summary>
public sealed class RegisterParticipationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/participations", HandleAsync)
            .WithName("RegisterParticipation")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Participations.Manage)
            .WithValidation<RegisterParticipationRequest>()
            .Produces<RegisterParticipationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        RegisterParticipationRequest request,
        RegisterParticipationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/participations/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
