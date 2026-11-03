namespace Elsasa.Sis.IntegrationTests.Fixtures;

using System.Data;
using System.Data.Common;
using Dapper;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Abstract base class for integration test classes (ADR-008).
/// Resets the database before each test run via Respawn, provides <see cref="HttpClient"/> access,
/// and provides helper methods for executing raw Dapper queries against the container database.
/// </summary>
public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
{
    private readonly Lazy<HttpClient> _lazyClient;

    protected IntegrationTestWebAppFactory Factory { get; }

    protected HttpClient Client => _lazyClient.Value;

    protected ISqlConnectionFactory ConnectionFactory =>
        Factory.Services.GetRequiredService<ISqlConnectionFactory>();

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Factory = factory;
        _lazyClient = new Lazy<HttpClient>(() => factory.CreateClient());
    }

    /// <summary>
    /// Executes before each test: initializes the web app factory and resets database tables via Respawn.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await Factory.InitializeAsync();

        if (Factory.ContainerFixture.IsAvailable)
        {
            await Factory.ContainerFixture.ResetDatabaseAsync();
        }

        Factory.CurrentUser.Reset();
        Factory.EventCollector.Reset();
        Factory.FinanceModule.Reset();
    }

    /// <summary>
    /// Executes after each test.
    /// </summary>
    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Gracefully skips the calling test if the SQL Server database/container environment is unavailable.
    /// </summary>
    protected void SkipIfDatabaseUnavailable()
    {
        Skip.If(
            !Factory.ContainerFixture.IsAvailable,
            Factory.ContainerFixture.UnavailableReason ?? "SQL Server container/database environment is not available.");
    }

    /// <summary>
    /// Creates and opens a new connection to the container database.
    /// </summary>
    protected async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        SkipIfDatabaseUnavailable();

        var connection = new SqlConnection(Factory.ContainerFixture.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// Executes a command against the container database using Dapper.
    /// </summary>
    protected async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(
            sql,
            param,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Executes a scalar query against the container database using Dapper.
    /// </summary>
    protected async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(
            sql,
            param,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Queries the container database using Dapper and returns typed records.
    /// </summary>
    protected async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.QueryAsync<T>(new CommandDefinition(
            sql,
            param,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Queries a single record or default from the container database using Dapper.
    /// </summary>
    protected async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(
            sql,
            param,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Queries a single required record from the container database using Dapper.
    /// </summary>
    protected async Task<T> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<T>(new CommandDefinition(
            sql,
            param,
            transaction,
            commandTimeout,
            commandType,
            cancellationToken: cancellationToken));
    }
}
