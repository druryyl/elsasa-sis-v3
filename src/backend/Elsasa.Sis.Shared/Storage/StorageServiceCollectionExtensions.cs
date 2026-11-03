namespace Elsasa.Sis.Shared.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection extensions for registering file storage infrastructure services.
/// </summary>
public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IFileStorageService"/> and its configuration options into the service container.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddSharedStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<IFileStorageService>(sp =>
            new PhysicalFileStorageService(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageOptions>>()));

        return services;
    }
}
