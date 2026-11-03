namespace Elsasa.Sis.Students.Features.Admission.WithdrawAdmissionApplication;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for withdrawing an admission application (ADR-005).
/// </summary>
public sealed class WithdrawAdmissionApplicationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/admission/applications/{id:guid}/withdraw", HandleAsync)
            .WithName("WithdrawAdmissionApplication")
            .WithTags("AdmissionApplications")
            .RequireAuthorization(Permissions.Students.Admission.Register)
            .WithValidation<WithdrawAdmissionApplicationRequest>()
            .Produces<WithdrawAdmissionApplicationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        WithdrawAdmissionApplicationRequest request,
        WithdrawAdmissionApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
