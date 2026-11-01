namespace Elsasa.Sis.Api.Configuration;

using System.Reflection;
using Elsasa.Sis.Api.Services;
using Elsasa.Sis.Contracts;
using Elsasa.Sis.Finance;
using Elsasa.Sis.Operations;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Students;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Root dependency injection composition for the ASP.NET Core host and Stage 1 domain assemblies (ADR-001).
/// </summary>
public static class DependencyInjection
{
    public const string DefaultConnectionKey = "DefaultConnection";

    /// <summary>
    /// The assemblies participating in cross-domain events, contracts, and mediator messaging.
    /// </summary>
    public static readonly Assembly[] DomainAssemblies =
    [
        typeof(OperationsAssemblyMarker).Assembly,
        typeof(StudentsAssemblyMarker).Assembly,
        typeof(FinanceAssemblyMarker).Assembly,
        typeof(ContractsAssemblyMarker).Assembly,
        typeof(Program).Assembly,
    ];

    /// <summary>
    /// The Stage 1 domain assemblies participating in endpoint discovery and Minimal API route mapping.
    /// </summary>
    public static readonly Assembly[] Stage1Assemblies =
    [
        typeof(OperationsAssemblyMarker).Assembly,
        typeof(StudentsAssemblyMarker).Assembly,
        typeof(FinanceAssemblyMarker).Assembly,
    ];

    /// <summary>
    /// Registers host runtime dependencies, Dapper type handlers, MediatR, FluentValidation,
    /// current user context, database connection factory, and health checks.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddHostComposition(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Core hosting prerequisites
        services.AddOptions();
        services.AddLogging();

        // 1. Dapper custom type handlers
        DapperTypeHandlers.RegisterTypeHandlers();

        // 2. Database connection factory & health checks
        var connectionString = configuration.GetConnectionString(DefaultConnectionKey);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
            services.AddHealthChecks()
                .AddSqlServer(connectionString, name: "sqlserver", tags: ["db", "sql", "ready"]);
        }
        else
        {
            services.AddHealthChecks();
        }

        // 3. User context
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // 4. MediatR scanning domain assemblies
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(DomainAssemblies);
        });

        // 5. FluentValidation scanning domain assemblies
        services.AddValidatorsFromAssemblies(DomainAssemblies);

        // 6. Domain services & slice handlers
        services.AddOperationsDomain();

        return services;
    }
}
