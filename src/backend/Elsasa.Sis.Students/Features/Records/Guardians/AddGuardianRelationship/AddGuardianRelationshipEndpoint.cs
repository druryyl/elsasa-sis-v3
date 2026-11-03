namespace Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for linking a guardian relationship to a student (ADR-004, ADR-005).
/// </summary>
public sealed class AddGuardianRelationshipEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/{id:guid}/guardians", HandleAsync)
            .WithName("AddGuardianRelationship")
            .WithTags("StudentGuardians")
            .RequireAuthorization(Permissions.Students.Records.Edit)
            .WithValidation<AddGuardianRelationshipRequest>()
            .Produces<AddGuardianRelationshipResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        AddGuardianRelationshipRequest request,
        AddGuardianRelationshipHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/students/{id}/guardians/{result.Value.RelationshipId}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
