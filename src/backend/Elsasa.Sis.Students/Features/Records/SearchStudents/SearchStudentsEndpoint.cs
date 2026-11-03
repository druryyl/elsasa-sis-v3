namespace Elsasa.Sis.Students.Features.Records.SearchStudents;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for searching students (ADR-004, ADR-005).
/// </summary>
public sealed class SearchStudentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students", HandleAsync)
            .WithName("SearchStudents")
            .WithTags("Students")
            .RequireAuthorization(Permissions.Students.Records.View)
            .Produces<SearchStudentsResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        string? searchTerm,
        string? campus,
        string? level,
        string? status,
        int? pageNumber,
        int? pageSize,
        SearchStudentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            searchTerm,
            campus,
            level,
            status,
            pageNumber ?? 1,
            pageSize ?? 20,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
