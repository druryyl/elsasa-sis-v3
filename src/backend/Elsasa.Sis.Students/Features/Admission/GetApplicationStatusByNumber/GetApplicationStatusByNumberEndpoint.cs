namespace Elsasa.Sis.Students.Features.Admission.GetApplicationStatusByNumber;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for anonymous public application status lookup (ADR-004, ADR-005).
/// </summary>
public sealed class GetApplicationStatusByNumberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students/admission/applications/status", HandleAsync)
            .WithName("GetApplicationStatusByNumber")
            .WithTags("Admission")
            .AllowAnonymous()
            .Produces<ApplicationStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        string? applicationNumber,
        DateOnly? dateOfBirth,
        GetApplicationStatusByNumberHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(applicationNumber, dateOfBirth, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
