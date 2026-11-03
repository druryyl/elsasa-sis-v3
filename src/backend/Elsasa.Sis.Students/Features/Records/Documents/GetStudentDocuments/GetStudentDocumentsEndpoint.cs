namespace Elsasa.Sis.Students.Features.Records.Documents.GetStudentDocuments;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Elsasa.Sis.Students.Features.Records.GetStudentById;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving official student documents (ADR-004, ADR-005, P4-S06).
/// </summary>
public sealed class GetStudentDocumentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students/{id:guid}/documents", HandleAsync)
            .WithName("GetStudentDocuments")
            .WithTags("Students")
            .RequireAuthorization(Permissions.Students.Records.View)
            .Produces<IReadOnlyList<StudentDocumentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetStudentDocumentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
