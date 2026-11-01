namespace Elsasa.Sis.Operations;

using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection registrations for the Operations domain assembly (ADR-001).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all feature slice handlers and domain services for the Operations bounded context.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddOperationsDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<CreateAcademicYearHandler>();

        return services;
    }
}
