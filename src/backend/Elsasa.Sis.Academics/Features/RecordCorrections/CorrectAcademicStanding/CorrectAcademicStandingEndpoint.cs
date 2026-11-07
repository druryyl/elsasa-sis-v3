namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;

using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Academic Authorities to perform auditable in-place corrections to approved academic standings (P5-S12, UC-ACD-15, TD-05).
/// </summary>
public sealed class CorrectAcademicStandingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/academic-standings/{id:guid}/correct", HandleAsync)
            .WithName("CorrectAcademicStanding")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Corrections.CorrectStanding)
            .WithValidation<CorrectAcademicStandingRequest>()
            .Produces<StudentAcademicStandingDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CorrectAcademicStandingRequest request,
        CorrectAcademicStandingHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CorrectAcademicStandingCommand(
            id,
            request.Standing,
            request.PolicyReference,
            request.Conditions,
            request.IsException,
            request.ExceptionReason,
            request.CommitteeReference,
            request.Reason);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
