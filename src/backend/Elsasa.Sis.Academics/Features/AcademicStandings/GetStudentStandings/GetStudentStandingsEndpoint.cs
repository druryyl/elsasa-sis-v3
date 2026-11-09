namespace Elsasa.Sis.Academics.Features.AcademicStandings.GetStudentStandings;

using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying historical standing determinations for a student (UC-ACD-17, P3-S09).
/// </summary>
public sealed class GetStudentStandingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/students/{studentId:guid}/standings", HandleAsync)
            .WithName("GetStudentStandings")
            .WithTags("AcademicStandings")
            .RequireAuthorization(Permissions.Academics.Standings.View)
            .Produces<IReadOnlyList<StudentAcademicStandingDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        GetStudentStandingsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(studentId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
