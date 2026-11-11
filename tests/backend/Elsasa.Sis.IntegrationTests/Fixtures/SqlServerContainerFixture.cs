namespace Elsasa.Sis.IntegrationTests.Fixtures;

using Elsasa.Sis.Database;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;
using Xunit;

/// <summary>
/// Manages the lifecycle of a disposable SQL Server 2022 container (Testcontainers) or connection
/// fallback, executes DbUp schema migrations upon startup, and provides Respawn database checkpointing (ADR-008).
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim _initLock = new(1, 1);
    private static MsSqlContainer? _sharedContainer;
    private static string? _sharedConnectionString;
    private static Respawner? _sharedRespawner;
    private static bool _staticInitialized;
    private static bool _staticIsAvailable;
    private static string? _staticUnavailableReason;

    public const string TestConnectionStringEnvVar = "TEST_SQLSERVER_CONNECTION_STRING";
    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";

    /// <summary>
    /// Gets a value indicating whether either a Docker container or fallback SQL Server database is available.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Gets a value indicating whether either a Docker container or fallback SQL Server database is available (static access).
    /// </summary>
    public static bool IsDockerOrDatabaseAvailable => _staticIsAvailable;

    /// <summary>
    /// Gets the reason why SQL Server is unavailable when <see cref="IsAvailable"/> is <see langword="false"/>.
    /// </summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>
    /// Gets the connection string pointing to the active SQL Server instance.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the active Respawn checkpoint instance configured for data resets.
    /// </summary>
    public Respawner? Respawner { get; private set; }

    public async Task InitializeAsync()
    {
        if (!_staticInitialized)
        {
            await _initLock.WaitAsync();
            try
            {
                if (!_staticInitialized)
                {
                    await EnsureSharedEnvironmentInitializedAsync();
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        IsAvailable = _staticIsAvailable;
        UnavailableReason = _staticUnavailableReason;
        ConnectionString = _sharedConnectionString ?? string.Empty;
        Respawner = _sharedRespawner;
    }

    /// <summary>
    /// Executes Respawn reset to clean all domain table data while preserving DbUp schema versions.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (!IsAvailable || Respawner is null || string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await Respawner.ResetAsync(connection);
    }

    public async Task DisposeAsync()
    {
        // Container lifecycle cleanup: Testcontainers Ryuk handles automated cleanup on process exit.
        await Task.CompletedTask;
    }

    private static async Task EnsureSharedEnvironmentInitializedAsync()
    {
        // 1. Check fallback environment variable TEST_SQLSERVER_CONNECTION_STRING
        var envConnectionString = Environment.GetEnvironmentVariable(TestConnectionStringEnvVar);
        if (!string.IsNullOrWhiteSpace(envConnectionString))
        {
            await InitializeFromConnectionStringAsync(envConnectionString);
            return;
        }

        // 2. Fast check: Is Docker engine running or reachable?
        if (!IsDockerEngineAvailable())
        {
            _staticIsAvailable = false;
            _staticUnavailableReason = "Docker runtime is not available and TEST_SQLSERVER_CONNECTION_STRING is not set.";
            _staticInitialized = true;
            return;
        }

        // 3. Docker is detected: build and boot Testcontainer
        try
        {
            var container = new MsSqlBuilder(SqlServerImage)
                .WithCleanUp(true)
                .Build();

            await container.StartAsync();
            _sharedContainer = container;
            var connectionString = container.GetConnectionString();

            await InitializeFromConnectionStringAsync(connectionString);
        }
        catch (Exception ex)
        {
            _staticIsAvailable = false;
            _staticUnavailableReason = $"Failed to start SQL Server Testcontainer: {ex.Message}";
            _staticInitialized = true;
        }
    }

    private static async Task InitializeFromConnectionStringAsync(string connectionString)
    {
        try
        {
            // Execute DatabaseMigrator against the target database
            var migrationSuccess = DatabaseMigrator.UpgradeDatabase(connectionString);
            if (!migrationSuccess)
            {
                _staticIsAvailable = false;
                _staticUnavailableReason = "DbUp database migration failed to execute cleanly on target SQL Server.";
                _staticInitialized = true;
                return;
            }

            // Initialize Respawn checkpointing targeting Stage 1 domain schemas while preserving _SchemaVersions
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            _sharedRespawner = await Respawn.Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                SchemasToInclude = ["operations", "students", "finance", "academics", "teaching"],
                TablesToIgnore =
                [
                    new Respawn.Graph.Table("dbo", "_SchemaVersions"),
                    new Respawn.Graph.Table("_SchemaVersions"),
                    // Master reference table seeded by DbUp migration 20261111_0201 must persist across test resets
                    new Respawn.Graph.Table("finance", "policy_categories"),
                ],
            });

            _sharedConnectionString = connectionString;
            _staticIsAvailable = true;
            _staticInitialized = true;
        }
        catch (Exception ex)
        {
            _staticIsAvailable = false;
            _staticUnavailableReason = $"Failed to configure database migrations or Respawn: {ex.Message}";
            _staticInitialized = true;
        }
    }

    private static bool IsDockerEngineAvailable()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
            {
                return true;
            }

            if (OperatingSystem.IsWindows())
            {
                const string pipePath = @"\\.\pipe\";
                if (Directory.Exists(pipePath))
                {
                    var pipes = Directory.GetFiles(pipePath, "docker*");
                    return pipes.Any(p =>
                        p.EndsWith("docker_engine", StringComparison.OrdinalIgnoreCase) ||
                        p.EndsWith("dockerDesktopLinuxEngine", StringComparison.OrdinalIgnoreCase));
                }

                return false;
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (File.Exists("/var/run/docker.sock"))
                {
                    return true;
                }

                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrWhiteSpace(home) && File.Exists(Path.Combine(home, ".docker", "run", "docker.sock")))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
