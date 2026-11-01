namespace Elsasa.Sis.Api.Middleware;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Global exception handler that catches unhandled system/infrastructure exceptions (ADR-007).
/// Logs the exception alongside correlation ID and request path, and returns an RFC 7807 500
/// ProblemDetails payload without exposing internal stack traces.
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var correlationId = ResolveCorrelationId(httpContext);
        var path = httpContext.Request.Path.Value ?? string.Empty;

        LogUnhandledException(_logger, exception, path, correlationId);

        if (httpContext.Response.HasStarted)
        {
            LogResponseAlreadyStarted(_logger);
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred while processing your request.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["correlationId"] = correlationId;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);

        return true;
    }

    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Response.Headers.TryGetValue(CorrelationIdMiddleware.CorrelationIdHeader, out var responseHeader) &&
            !string.IsNullOrWhiteSpace(responseHeader))
        {
            return responseHeader.ToString();
        }

        if (httpContext.Request.Headers.TryGetValue(CorrelationIdMiddleware.CorrelationIdHeader, out var requestHeader) &&
            !string.IsNullOrWhiteSpace(requestHeader))
        {
            return requestHeader.ToString();
        }

        if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var itemValue) &&
            itemValue is string s &&
            !string.IsNullOrWhiteSpace(s))
        {
            return s;
        }

        return httpContext.TraceIdentifier;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred while processing request {Path}. CorrelationId: {CorrelationId}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        Exception exception,
        string path,
        string correlationId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Response has already started; cannot write RFC 7807 500 ProblemDetails payload.")]
    private static partial void LogResponseAlreadyStarted(ILogger logger);
}

/// <summary>
/// Extension methods for configuring <see cref="GlobalExceptionHandler"/>.
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    /// <summary>
    /// Registers the <see cref="GlobalExceptionHandler"/> and default ProblemDetails services into DI.
    /// </summary>
    public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
