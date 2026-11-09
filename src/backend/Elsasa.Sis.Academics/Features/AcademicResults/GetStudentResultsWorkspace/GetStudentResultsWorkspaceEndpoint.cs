namespace Elsasa.Sis.Academics.Features.AcademicResults.GetStudentResultsWorkspace;

using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying published results in the student/family self-service learning workspace (SCR-SELF-03, P4-S10).
/// </summary>
public sealed class GetStudentResultsWorkspaceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/portal/learning/results", HandleAsync)
            .WithName("GetStudentResultsWorkspace")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Results.View)
            .Produces<IReadOnlyList<StudentPublishedResultDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid academicPeriodId,
        [FromQuery] Guid? studentId,
        GetStudentResultsWorkspaceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            academicPeriodId: academicPeriodId,
            studentIdOverride: studentId,
            cancellationToken: cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
