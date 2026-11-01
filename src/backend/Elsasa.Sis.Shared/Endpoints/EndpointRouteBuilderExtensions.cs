namespace Elsasa.Sis.Shared.Endpoints;

using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for discovering and mapping <see cref="IEndpoint"/> implementations (ADR-005).
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Scans the specified assemblies for non-abstract classes implementing <see cref="IEndpoint"/>,
    /// instantiates each endpoint, and invokes <see cref="IEndpoint.MapEndpoint(IEndpointRouteBuilder)"/>.
    /// If no assemblies are provided, the currently loaded assemblies in the AppDomain are scanned.
    /// </summary>
    /// <param name="app">The route builder on which to map endpoints.</param>
    /// <param name="assemblies">The assemblies to scan for endpoints. If empty, loaded assemblies are scanned.</param>
    /// <returns>The <paramref name="app"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> or <paramref name="assemblies"/> is null.</exception>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assemblies);

        var targetAssemblies = assemblies.Length == 0
            ? AppDomain.CurrentDomain.GetAssemblies()
            : assemblies;

        var endpointType = typeof(IEndpoint);

        var endpointTypes = targetAssemblies
            .Where(a => !a.IsDynamic)
            .Distinct()
            .SelectMany(GetExportedTypesSafely)
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && endpointType.IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            var endpoint = (IEndpoint)(type.GetConstructor(Type.EmptyTypes) is not null
                ? Activator.CreateInstance(type)!
                : (app.ServiceProvider is not null
                    ? ActivatorUtilities.CreateInstance(app.ServiceProvider, type)
                    : Activator.CreateInstance(type)!));

            endpoint.MapEndpoint(app);
        }

        return app;
    }

    private static IEnumerable<Type> GetExportedTypesSafely(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
