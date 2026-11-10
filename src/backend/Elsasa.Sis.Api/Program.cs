using Elsasa.Sis.Academics;
using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Api.Features.DemoData;
using Elsasa.Sis.Api.Middleware;
using Elsasa.Sis.Database;
using Elsasa.Sis.Teaching;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.Extensions.Logging;
using Serilog;
using DependencyInjection = Elsasa.Sis.Api.Configuration.DependencyInjection;

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

// Demo data seed/clean CLI support (ADR-001, ADR-006, TD-01, TD-02, TD-08, OQ-004)
if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase) || args.Contains("--clean-only", StringComparer.OrdinalIgnoreCase))
{
    var cleanOnly = args.Contains("--clean-only", StringComparer.OrdinalIgnoreCase);

    // Run pending migrations first (ADR-006)
    var migrationConnectionString = builder.Configuration.GetConnectionString(DependencyInjection.DefaultConnectionKey);
    var migrationStatus = Program.RunMigration(migrationConnectionString);
    if (migrationStatus != 0)
    {
        Console.Error.WriteLine("Demo data operation aborted: Database migration failed.");
        return 1;
    }

    // Build minimal host for demo data orchestration
    builder.Services.AddGlobalExceptionHandler();
    builder.Services.AddHostComposition(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddAcademicsDomain(builder.Configuration);
    builder.Services.AddTeachingModule(builder.Configuration);

    using var host = builder.Build();
    try
    {
        await host.StartAsync();

        using var scope = host.Services.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<DemoDataOrchestrator>();
        var cancellationToken = host.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>().ApplicationStopping;

        Console.WriteLine($"Running demo data operation (CleanOnly: {cleanOnly})...");
        var summary = await orchestrator.ExecuteResetAsync(cleanOnly, cancellationToken);

        Console.WriteLine($"Demo data reset completed successfully.");
        Console.WriteLine($"  Purged Schemas: {summary.PurgedSchemas}");
        Console.WriteLine($"  Seeded Domains: {summary.SeededDomains}");
        Console.WriteLine($"  Students: {summary.TotalStudentsSeeded}");
        Console.WriteLine($"  Invoices: {summary.TotalInvoicesSeeded}");
        Console.WriteLine($"  Attendance Records: {summary.TotalAttendanceSeeded}");
        Console.WriteLine($"  Results: {summary.TotalResultsSeeded}");
        Console.WriteLine($"  Report Cards: {summary.TotalReportCardsSeeded}");
        Console.WriteLine($"  Duration: {summary.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Anchored Date: {summary.AnchoredDate:yyyy-MM-dd}");
        Console.WriteLine($"  Academic Year: {summary.BaseAcademicYearCode}");

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Demo data operation failed: {ex.Message}");
        return 1;
    }
}

// Configure physical storage path for coursework submissions (TD-07, P5-S13)
var submissionStorageDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Submissions");
if (!Directory.Exists(submissionStorageDir))
{
    Directory.CreateDirectory(submissionStorageDir);
}

builder.Services.AddGlobalExceptionHandler();
builder.Services.AddHostComposition(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddAcademicsDomain(builder.Configuration);
builder.Services.AddTeachingModule(builder.Configuration);

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
app.MapAcademicsEndpoints();
app.MapTeachingEndpoints();
new Elsasa.Sis.Api.Features.Auth.LoginEndpoint().MapEndpoint(app);
new Elsasa.Sis.Api.Features.DemoData.DemoDataEndpoints().MapEndpoint(app);

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
