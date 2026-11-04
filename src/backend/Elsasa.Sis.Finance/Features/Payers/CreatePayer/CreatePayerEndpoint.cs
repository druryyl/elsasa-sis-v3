namespace Elsasa.Sis.Finance.Features.Payers.CreatePayer;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for creating a new payer entity (SCR-FIN-02, TD-03).
/// </summary>
public sealed class CreatePayerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/payers", HandleAsync)
            .WithName("CreatePayer")
            .WithTags("FinancePayers")
            .RequireAuthorization(Permissions.Finance.Payers.Manage)
            .WithValidation<CreatePayerRequest>()
            .Produces<CreatePayerResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        CreatePayerRequest request,
        CreatePayerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/finance/payers/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
