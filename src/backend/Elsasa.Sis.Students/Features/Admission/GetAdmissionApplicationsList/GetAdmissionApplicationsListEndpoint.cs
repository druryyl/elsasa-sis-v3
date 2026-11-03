namespace Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationsList;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for listing admission queue applications (ADR-004, ADR-005).
/// </summary>
public sealed class GetAdmissionApplicationsListEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students/admission/applications", HandleAsync)
            .WithName("GetAdmissionApplicationsList")
            .WithTags("Admission")
            .RequireAuthorization(Permissions.Students.Admission.View)
            .Produces<IReadOnlyList<AdmissionApplicationListItemResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        Guid? admissionPeriodId,
        string? status,
        string? requestedEntryLevel,
        string? searchTerm,
        int page,
        int pageSize,
        GetAdmissionApplicationsListHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            admissionPeriodId,
            status,
            requestedEntryLevel,
            searchTerm,
            page,
            pageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
