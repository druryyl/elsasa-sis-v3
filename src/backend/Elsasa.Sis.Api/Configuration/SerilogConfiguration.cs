namespace Elsasa.Sis.Api.Configuration;

using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Formatting.Json;

/// <summary>
/// Configures Serilog structured JSON console logging and standard enrichers (ADR-007).
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Enriches a <see cref="LoggerConfiguration"/> with standard enrichers
    /// (<c>FromLogContext</c>, <c>WithMachineName</c>, <c>WithEnvironmentName</c>,
    /// <c>WithThreadId</c>, <c>ProcessId</c>) and JSON console formatting.
    /// </summary>
    public static LoggerConfiguration ConfigureStandardLogging(this LoggerConfiguration loggerConfiguration)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);

        return loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .WriteTo.Console(new JsonFormatter());
    }

    /// <summary>
    /// Configures Serilog as the logging provider on the specified <see cref="WebApplicationBuilder"/>.
    /// </summary>
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .ConfigureStandardLogging();
        });

        return builder;
    }
}
