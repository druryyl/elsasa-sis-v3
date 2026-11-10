namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.RegisterTeacherProfile;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for candidate teacher profile intake (ADR-004, ADR-005, P2-S05).
/// </summary>
public sealed class RegisterTeacherProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/teachers", HandleAsync)
            .WithName("RegisterTeacherProfile")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Manage)
            .WithValidation<RegisterTeacherProfileRequest>()
            .Produces<RegisterTeacherProfileResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        RegisterTeacherProfileRequest request,
        RegisterTeacherProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/teachers/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
