namespace Elsasa.Sis.Finance;

using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Finance.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection registrations for the Finance domain assembly (ADR-001).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Finance domain services exposed to other bounded contexts.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddFinanceDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Cross-domain query contract (ADR-003)
        services.AddScoped<IFinanceModule, FinanceModule>();

        return services;
    }
}
