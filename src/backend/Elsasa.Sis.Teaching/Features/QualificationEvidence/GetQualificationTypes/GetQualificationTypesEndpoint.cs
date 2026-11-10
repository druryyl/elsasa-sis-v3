namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.GetQualificationTypes;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying recognized active qualification types (TD-02, P3-S09).
/// </summary>
public sealed class GetQualificationTypesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/qualification-types", HandleAsync)
            .WithName("GetQualificationTypes")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.View)
            .Produces<IReadOnlyList<QualificationTypeResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromServices] GetQualificationTypesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
