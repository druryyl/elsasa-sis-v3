using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.SqlServer;
using Microsoft.Extensions.Logging;

namespace Elsasa.Sis.Database;

/// <summary>
/// Applies the forward-only, embedded T-SQL migration scripts that provision the
/// Stage 1 SQL Server schemas and tables (ADR-002, ADR-006).
/// </summary>
/// <remarks>
/// The runner is deliberately free of domain knowledge and SIS project
/// references: it is invoked by the web host at startup (Development/Testing) or
/// via a dedicated migration flag, and by the integration test harness.
/// </remarks>
public static class DatabaseMigrator
{
    /// <summary>
    /// Schema that owns the DbUp journal table. Kept in the default schema so
    /// later Respawn-based test cleanup can preserve it while truncating domain
    /// tables.
    /// </summary>
    private const string _journalSchema = "dbo";

    /// <summary>DbUp journal table name (created and maintained by DbUp).</summary>
    private const string _journalTableName = "_SchemaVersions";

    /// <summary>
    /// Resource-name fragment that identifies an embedded script as a migration
    /// script, e.g. "Elsasa.Sis.Database.Scripts.20261101_0001_Init_Schemas.sql".
    /// </summary>
    private const string _scriptsFolderSegment = ".Scripts.";

    private const string _scriptsFolderPrefix = "Scripts.";

    /// <summary>
    /// Applies every embedded migration script that has not been journaled yet,
    /// in deterministic filename order of their <c>YYYYMMDD_NNNN_</c> prefix.
    /// </summary>
    /// <param name="connectionString">Connection string for the target database.</param>
    /// <param name="logger">
    /// Optional logger. When supplied, every DbUp log entry (script started,
    /// script applied, errors) is forwarded to it; when omitted, migration is
    /// silent. Errors are always reported through this logger and never
    /// swallowed silently.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when every pending script was applied successfully;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool UpgradeDatabase(string connectionString, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        IUpgradeLog upgradeLog = logger is null
            ? new NoOpUpgradeLog()
            : new MicrosoftUpgradeLog(logger);

        DatabaseUpgradeResult result;

        try
        {
            var upgrader = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(
                    typeof(DatabaseMigrator).Assembly,
                    IsMigrationScriptResource)
                .JournalTo((connectionManagerFactory, logFactory) =>
                    new SqlTableJournal(connectionManagerFactory, logFactory, _journalSchema, _journalTableName))
                .WithTransactionPerScript()
                .LogTo(upgradeLog)
                .Build();

            result = upgrader.PerformUpgrade();
        }
        catch (Exception exception)
        {
            // Infrastructure failures (connection drop, permissions, ...) surface
            // as exceptions from DbUp; report them instead of hiding them.
            upgradeLog.LogError(exception, "Database migration failed: {Message}", exception.Message);
            return false;
        }

        if (!result.Successful)
        {
            if (result.Error is not null)
            {
                upgradeLog.LogError(result.Error, "Database migration failed: {Message}", result.Error.Message);
            }
            else
            {
                upgradeLog.LogError("Database migration failed without a reported error.");
            }

            return false;
        }

        upgradeLog.LogInformation(
            "Database migration completed successfully; {ScriptCount} script(s) applied.",
            result.Scripts.Count());

        return true;
    }

    /// <summary>
    /// Selects only the embedded resources that live in the <c>Scripts</c>
    /// folder, so unrelated embedded resources (if any are added later) are never
    /// executed as migrations.
    /// </summary>
    /// <param name="resourceName">Manifest resource name of an embedded resource.</param>
    /// <returns><see langword="true"/> when the resource is a migration script.</returns>
    private static bool IsMigrationScriptResource(string resourceName) =>
        resourceName.Contains(_scriptsFolderSegment, StringComparison.OrdinalIgnoreCase)
        || resourceName.StartsWith(_scriptsFolderPrefix, StringComparison.OrdinalIgnoreCase);
}
