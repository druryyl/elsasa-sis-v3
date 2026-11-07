namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying paginated and filterable learning plan roster (SCR-ACD-04, P2-S06).
/// </summary>
public sealed class GetStudentLearningPlansEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-plans", HandleAsync)
            .WithName("GetStudentLearningPlans")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.View)
            .Produces<PagedResult<StudentLearningPlanListItemDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        Guid? academicPeriodId,
        string? status,
        string? studentSearch,
        int? page,
        int? pageSize,
        GetStudentLearningPlansHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetStudentLearningPlansQuery(
            AcademicPeriodId: academicPeriodId,
            Status: status,
            StudentSearch: studentSearch,
            Page: page ?? 1,
            PageSize: pageSize ?? 20);

        var result = await handler.HandleAsync(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
