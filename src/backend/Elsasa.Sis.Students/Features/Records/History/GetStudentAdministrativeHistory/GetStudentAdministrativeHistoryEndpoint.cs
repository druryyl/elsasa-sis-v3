namespace Elsasa.Sis.Students.Features.Records.History.GetStudentAdministrativeHistory;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving a student's administrative history audit log (GAP-006, TD-06).
/// </summary>
public sealed class GetStudentAdministrativeHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students/{id:guid}/history", HandleAsync)
            .WithName("GetStudentAdministrativeHistory")
            .WithTags("Students")
            .RequireAuthorization(Permissions.Students.Records.View)
            .Produces<GetStudentAdministrativeHistoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetStudentAdministrativeHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
