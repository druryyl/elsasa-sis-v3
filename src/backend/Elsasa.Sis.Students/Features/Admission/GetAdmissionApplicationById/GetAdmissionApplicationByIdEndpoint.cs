namespace Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationById;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving an admission intake dossier by application ID (ADR-004, ADR-005).
/// </summary>
public sealed class GetAdmissionApplicationByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students/admission/applications/{id:guid}", HandleAsync)
            .WithName("GetAdmissionApplicationById")
            .WithTags("Admission")
            .RequireAuthorization(Permissions.Students.Admission.View)
            .Produces<AdmissionApplicationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetAdmissionApplicationByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
