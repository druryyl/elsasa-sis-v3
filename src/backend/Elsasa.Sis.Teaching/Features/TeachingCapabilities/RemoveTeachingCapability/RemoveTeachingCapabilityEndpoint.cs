namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.RemoveTeachingCapability;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for retiring a teaching capability from a teacher profile (P2-S08).
/// </summary>
public sealed class RemoveTeachingCapabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete("/api/v1/teaching/teachers/{id:guid}/capabilities/{capabilityId:guid}", HandleAsync)
            .WithName("RemoveTeachingCapability")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid capabilityId,
        RemoveTeachingCapabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, capabilityId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToProblemDetails();
    }
}
