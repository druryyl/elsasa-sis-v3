namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.GetAcademicYearById;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving an academic year by ID (ADR-005).
/// </summary>
public sealed class GetAcademicYearByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/academic-years/{id:guid}", HandleAsync)
            .WithName("GetAcademicYearById")
            .WithTags("AcademicYears")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<GetAcademicYearByIdResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetAcademicYearByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
