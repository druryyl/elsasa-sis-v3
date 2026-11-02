namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for creating an academic year (ADR-005).
/// </summary>
public sealed class CreateAcademicYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/academic-years", HandleAsync)
            .WithName("CreateAcademicYear")
            .WithTags("AcademicYears")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .WithValidation<CreateAcademicYearRequest>()
            .Produces<CreateAcademicYearResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        CreateAcademicYearRequest request,
        CreateAcademicYearHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/operations/academic-years/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
