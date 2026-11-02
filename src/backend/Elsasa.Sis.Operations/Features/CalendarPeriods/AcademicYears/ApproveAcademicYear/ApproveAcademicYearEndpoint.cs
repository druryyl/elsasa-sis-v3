namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApproveAcademicYear;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for approving an academic year (ADR-005).
/// </summary>
public sealed class ApproveAcademicYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/academic-years/{id:guid}/approve", HandleAsync)
            .WithName("ApproveAcademicYear")
            .WithTags("AcademicYears")
            .RequireAuthorization(Permissions.Operations.Calendar.Approve)
            .Produces<ApproveAcademicYearResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApproveAcademicYearHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
