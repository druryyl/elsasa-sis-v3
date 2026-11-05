namespace Elsasa.Sis.Academics.Features.AcademicParticipations.GetParticipationsByUnit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying participating students roster of a learning unit (ADR-004, ADR-005, P2-S06, TD-08).
/// </summary>
public sealed class GetParticipationsByUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-units/{learningUnitId:guid}/participations", HandleAsync)
            .WithName("GetParticipationsByUnit")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Participations.View)
            .Produces<IReadOnlyList<UnitParticipationItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid learningUnitId,
        Guid? academicPeriodId,
        string? sectionLabel,
        string? status,
        GetParticipationsByUnitHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            learningUnitId,
            academicPeriodId,
            sectionLabel,
            status,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
