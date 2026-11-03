namespace Elsasa.Sis.Students.Features.Admission.RegisterApplication;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for staff-assisted and portal self-service application registration (ADR-004, ADR-005).
/// </summary>
public sealed class RegisterApplicationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // 1. Staff-assisted admission intake endpoint (secured by Permissions.Students.Admission.Register)
        app.MapPost("/api/students/admission/applications", HandleStaffRegistrationAsync)
            .WithName("RegisterAdmissionApplicationStaff")
            .WithTags("Admission")
            .RequireAuthorization(Permissions.Students.Admission.Register)
            .WithValidation<RegisterApplicationRequest>()
            .Produces<RegisterApplicationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 2. Public admission portal self-service endpoint (accessible anonymously)
        app.MapPost("/api/students/admission/portal/applications", HandlePortalRegistrationAsync)
            .WithName("RegisterAdmissionApplicationPortal")
            .WithTags("AdmissionPortal")
            .AllowAnonymous()
            .WithValidation<RegisterApplicationRequest>()
            .Produces<RegisterApplicationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleStaffRegistrationAsync(
        RegisterApplicationRequest request,
        RegisterApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/students/admission/applications/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }

    private static async Task<IResult> HandlePortalRegistrationAsync(
        RegisterApplicationRequest request,
        RegisterApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        // Enforce that anonymous public portal registrations cannot bypass window checks
        var sanitizedRequest = request with { BypassWindowCheck = false };
        var result = await handler.HandleAsync(sanitizedRequest, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/students/admission/applications/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
