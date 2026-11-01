using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Api.Middleware;
using Elsasa.Sis.Database;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureSerilog();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    var connectionString = builder.Configuration.GetConnectionString(DependencyInjection.DefaultConnectionKey);
    return Program.RunMigration(connectionString);
}

builder.Services.AddGlobalExceptionHandler();
builder.Services.AddHostComposition(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled", false))
{
    app.UseSwaggerDocumentation();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapEndpoints(DependencyInjection.Stage1Assemblies);

app.Run();

return 0;

/// <summary>
/// Public entry-point marker so integration tests can reference the web host
/// assembly through <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Executes the database migration runner outside of HTTP host lifetime (ADR-006).
    /// </summary>
    /// <param name="connectionString">SQL Server connection string to migrate.</param>
    /// <param name="logger">Optional logger for tracking migration progress.</param>
    /// <returns>0 if successful, non-zero if failed.</returns>
    public static int RunMigration(string? connectionString, Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("Database migration failed: Connection string 'DefaultConnection' was not configured.");
            return 1;
        }

        try
        {
            using var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
            var effectiveLogger = logger ?? loggerFactory.CreateLogger("DatabaseMigrator");
            var success = DatabaseMigrator.UpgradeDatabase(connectionString, effectiveLogger);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Database migration unhandled failure: {ex.Message}");
            return 1;
        }
    }
}
