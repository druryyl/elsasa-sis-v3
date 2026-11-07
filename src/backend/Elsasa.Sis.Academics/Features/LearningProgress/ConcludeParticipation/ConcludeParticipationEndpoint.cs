namespace Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for formally concluding learning unit participation (P2-S04, UC-ACD-12).
/// </summary>
public sealed class ConcludeParticipationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-progress/conclude", HandleAsync)
            .WithName("ConcludeLearningProgressParticipation")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Progress.Conclude)
            .WithValidation<ConcludeParticipationCommand>()
            .Produces<ConcludeParticipationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        ConcludeParticipationCommand command,
        ConcludeParticipationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
