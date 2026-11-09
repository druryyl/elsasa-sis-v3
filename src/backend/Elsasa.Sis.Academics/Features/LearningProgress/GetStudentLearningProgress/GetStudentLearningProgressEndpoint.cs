namespace Elsasa.Sis.Academics.Features.LearningProgress.GetStudentLearningProgress;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying student learning progress ledger (P2-S05, UC-ACD-12, SCR-ACD-07).
/// </summary>
public sealed class GetStudentLearningProgressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/students/{studentId:guid}/learning-progress", HandleAsync)
            .WithName("GetStudentLearningProgress")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Progress.View)
            .Produces<IReadOnlyList<StudentLearningProgressItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        Guid? academicPeriodId,
        GetStudentLearningProgressHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            studentId,
            academicPeriodId,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
