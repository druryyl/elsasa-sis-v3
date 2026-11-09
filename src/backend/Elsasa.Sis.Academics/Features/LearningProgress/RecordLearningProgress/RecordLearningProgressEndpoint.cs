namespace Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for recording or updating student learning progress (P2-S04, UC-ACD-12).
/// </summary>
public sealed class RecordLearningProgressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-progress", HandleAsync)
            .WithName("RecordLearningProgress")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Progress.Record)
            .WithValidation<RecordLearningProgressCommand>()
            .Produces<RecordLearningProgressResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        RecordLearningProgressCommand command,
        RecordLearningProgressHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
