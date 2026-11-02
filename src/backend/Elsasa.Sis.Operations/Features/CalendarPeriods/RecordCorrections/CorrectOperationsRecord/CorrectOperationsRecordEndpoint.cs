namespace Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.CorrectOperationsRecord;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for submitting auditable record corrections (ADR-005, UC-OPS-13).
/// </summary>
public sealed class CorrectOperationsRecordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/records/correct", HandleAsync)
            .WithName("CorrectOperationsRecord")
            .WithTags("RecordCorrections")
            .RequireAuthorization(Permissions.Operations.Records.Correct)
            .WithValidation<CorrectOperationsRecordRequest>()
            .Produces<CorrectOperationsRecordResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        CorrectOperationsRecordRequest request,
        CorrectOperationsRecordHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
