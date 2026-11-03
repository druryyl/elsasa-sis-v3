namespace Elsasa.Sis.Students.Features.Records.UpdateStudentIdentity;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for updating certified student identity (ADR-004, ADR-005).
/// </summary>
public sealed class UpdateStudentIdentityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/students/{id:guid}/identity", HandleAsync)
            .WithName("UpdateStudentIdentity")
            .WithTags("StudentRecords")
            .RequireAuthorization(Permissions.Students.Records.Edit)
            .WithValidation<UpdateStudentIdentityRequest>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateStudentIdentityRequest request,
        UpdateStudentIdentityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.Error.ToProblemDetails();
    }
}
