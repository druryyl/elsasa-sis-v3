namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for creating an operational period (ADR-005).
/// </summary>
public sealed class CreateOperationalPeriodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/academic-years/{academicYearId:guid}/periods", HandleAsync)
            .WithName("CreateOperationalPeriod")
            .WithTags("OperationalPeriods")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .WithValidation<CreateOperationalPeriodRequest>()
            .Produces<CreateOperationalPeriodResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid academicYearId,
        CreateOperationalPeriodRequest request,
        CreateOperationalPeriodHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicYearId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/operations/periods/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
