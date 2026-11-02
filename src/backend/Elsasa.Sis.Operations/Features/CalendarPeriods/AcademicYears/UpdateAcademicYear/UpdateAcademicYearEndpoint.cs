namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.UpdateAcademicYear;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for updating an academic year (ADR-005).
/// </summary>
public sealed class UpdateAcademicYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/operations/academic-years/{id:guid}", HandleAsync)
            .WithName("UpdateAcademicYear")
            .WithTags("AcademicYears")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .WithValidation<UpdateAcademicYearRequest>()
            .Produces<UpdateAcademicYearResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateAcademicYearRequest request,
        UpdateAcademicYearHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
