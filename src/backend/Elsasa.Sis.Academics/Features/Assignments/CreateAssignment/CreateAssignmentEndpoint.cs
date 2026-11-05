namespace Elsasa.Sis.Academics.Features.Assignments.CreateAssignment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for creating a coursework assignment tied to a learning activity (TD-05, UC-ACD-04, P5-S10).
/// </summary>
public sealed class CreateAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-activities/{activityId:guid}/assignments", HandleAsync)
            .WithName("CreateAssignment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assignments.Manage)
            .WithValidation<CreateAssignmentRequest>()
            .Produces<CreateAssignmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid activityId,
        CreateAssignmentRequest request,
        CreateAssignmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(activityId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/assignments/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
