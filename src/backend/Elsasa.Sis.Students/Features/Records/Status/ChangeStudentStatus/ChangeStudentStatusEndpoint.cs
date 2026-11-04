namespace Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for operational student status mutation (GAP-005, TD-05).
/// </summary>
public sealed class ChangeStudentStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/{id:guid}/status", HandleAsync)
            .WithName("ChangeStudentStatus")
            .WithTags("StudentRecords")
            .RequireAuthorization(Permissions.Students.Records.Edit)
            .WithValidation<ChangeStudentStatusRequest>()
            .Produces<ChangeStudentStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ChangeStudentStatusRequest request,
        ChangeStudentStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
