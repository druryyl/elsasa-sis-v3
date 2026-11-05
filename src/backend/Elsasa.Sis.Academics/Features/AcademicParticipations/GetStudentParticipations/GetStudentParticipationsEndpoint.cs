namespace Elsasa.Sis.Academics.Features.AcademicParticipations.GetStudentParticipations;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying unit participations for a student (ADR-004, ADR-005, P2-S06, TD-08).
/// </summary>
public sealed class GetStudentParticipationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/students/{studentId:guid}/participations", HandleAsync)
            .WithName("GetStudentParticipations")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Participations.View)
            .Produces<IReadOnlyList<StudentParticipationItemDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        Guid? academicPeriodId,
        GetStudentParticipationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            studentId,
            academicPeriodId,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
