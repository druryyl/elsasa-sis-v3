namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.UpdateTeacherProfile;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for updating mutable teacher profile details (ADR-004, ADR-005, P2-S05).
/// </summary>
public sealed class UpdateTeacherProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/v1/teaching/teachers/{id:guid}", HandleAsync)
            .WithName("UpdateTeacherProfile")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Manage)
            .WithValidation<UpdateTeacherProfileRequest>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateTeacherProfileRequest request,
        UpdateTeacherProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.Error.ToProblemDetails();
    }
}
