namespace Elsasa.Sis.Students.Features.Records.Guardians.UpdateGuardianRelationship;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for updating an existing student-guardian relationship (ADR-004, ADR-005).
/// </summary>
public sealed class UpdateGuardianRelationshipEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/students/{id:guid}/guardians/{relationshipId:guid}", HandleAsync)
            .WithName("UpdateGuardianRelationship")
            .WithTags("StudentGuardians")
            .RequireAuthorization(Permissions.Students.Records.Edit)
            .WithValidation<UpdateGuardianRelationshipRequest>()
            .Produces<UpdateGuardianRelationshipResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid relationshipId,
        UpdateGuardianRelationshipRequest request,
        UpdateGuardianRelationshipHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, relationshipId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
