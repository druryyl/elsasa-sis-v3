namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.AddTeachingCapability;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for recording a potential teaching capability on a teacher profile (P2-S08).
/// </summary>
public sealed class AddTeachingCapabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/teachers/{id:guid}/capabilities", HandleAsync)
            .WithName("AddTeachingCapability")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Manage)
            .WithValidation<AddTeachingCapabilityRequest>()
            .Produces<AddTeachingCapabilityResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        AddTeachingCapabilityRequest request,
        AddTeachingCapabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/teachers/{id}/capabilities/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
