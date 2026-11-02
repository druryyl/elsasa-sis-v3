namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApplyAcademicYearPreset;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for applying academic year period presets (ADR-005).
/// </summary>
public sealed class ApplyAcademicYearPresetEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/academic-years/{id:guid}/apply-preset", HandleAsync)
            .WithName("ApplyAcademicYearPreset")
            .WithTags("AcademicYears")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .Produces<ApplyAcademicYearPresetResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApplyAcademicYearPresetRequest? request,
        ApplyAcademicYearPresetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/operations/academic-years/{id}/periods", result.Value)
            : result.Error.ToProblemDetails();
    }
}
