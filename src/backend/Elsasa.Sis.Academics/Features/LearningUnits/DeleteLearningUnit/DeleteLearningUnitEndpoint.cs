namespace Elsasa.Sis.Academics.Features.LearningUnits.DeleteLearningUnit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for deleting a learning unit (ADR-004, ADR-005, P3-S08).
/// </summary>
public sealed class DeleteLearningUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete("/api/v1/academics/learning-units/{id:guid}", HandleAsync)
            .WithName("DeleteLearningUnit")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        DeleteLearningUnitHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToProblemDetails();
    }
}
