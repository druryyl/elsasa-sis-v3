namespace Elsasa.Sis.Api.Middleware;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

/// <summary>
/// Middleware that traces incoming HTTP requests with an RFC-compliant correlation identifier (ADR-007).
/// Extracts the identifier from incoming <c>X-Correlation-ID</c> header or generates a new unique Guid,
/// pushes it into Serilog's <see cref="LogContext"/>, and appends it to outgoing response headers.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-ID";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Items[CorrelationIdItemKey] = correlationId;

        // Eagerly attach to response headers for unit tests and early observers
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Ensure header is written if response begins asynchronously
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
            }

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(CorrelationIdItemKey, correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValues))
        {
            var candidate = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension methods for registering <see cref="CorrelationIdMiddleware"/> in the ASP.NET Core pipeline.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds <see cref="CorrelationIdMiddleware"/> to the application's request pipeline.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
