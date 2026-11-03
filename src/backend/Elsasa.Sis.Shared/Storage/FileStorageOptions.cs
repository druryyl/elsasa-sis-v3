namespace Elsasa.Sis.Shared.Storage;

/// <summary>
/// Configuration options for the physical file storage service.
/// </summary>
public sealed class FileStorageOptions
{
    /// <summary>
    /// Configuration section name in application configuration.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Gets or sets the local filesystem root directory for uploaded files.
    /// Defaults to <c>App_Data/storage</c>.
    /// </summary>
    public string LocalRootPath { get; set; } = "App_Data/storage";
}
