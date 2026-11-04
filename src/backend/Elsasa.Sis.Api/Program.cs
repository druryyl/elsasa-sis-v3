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
    var overrideArg = args.FirstOrDefault(a => a.StartsWith("--ConnectionStrings:DefaultConnection=", StringComparison.OrdinalIgnoreCase));
    var connectionString = overrideArg is not null
        ? overrideArg["--ConnectionStrings:DefaultConnection=".Length..]
        : builder.Configuration.GetConnectionString(DependencyInjection.DefaultConnectionKey);
    return Program.RunMigration(connectionString);
}

builder.Services.AddGlobalExceptionHandler();
builder.Services.AddHostComposition(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Apply pending database migrations automatically in Development and Testing
// environments (ADR-006), so a stale local database schema never surfaces as
// a 500 Internal Server Error at runtime.
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    var migrationConnectionString = builder.Configuration.GetConnectionString(DependencyInjection.DefaultConnectionKey);
    if (!string.IsNullOrWhiteSpace(migrationConnectionString))
    {
        using var migrationLoggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
        var migrationLogger = migrationLoggerFactory.CreateLogger("DatabaseMigrator");
        Program.RunMigration(migrationConnectionString, migrationLogger);
    }
}

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
new Elsasa.Sis.Api.Features.Auth.LoginEndpoint().MapEndpoint(app);

// Provide a landing page so the default browser URL (root path) does not return 404.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled", false))
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    app.MapGet("/", () => Results.Ok("Elsasa SIS API")).ExcludeFromDescription();
}

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
