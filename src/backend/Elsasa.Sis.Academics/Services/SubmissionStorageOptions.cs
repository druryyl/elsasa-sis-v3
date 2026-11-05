namespace Elsasa.Sis.Academics.Services;

/// <summary>
/// Configuration options for local coursework submission file storage (GAP-006, OQ-004, TD-07, P5-S13).
/// </summary>
public sealed class SubmissionStorageOptions
{
    /// <summary>
    /// Configuration section key in application configuration.
    /// </summary>
    public const string SectionName = "Academics:Submissions";

    /// <summary>
    /// Root path for storing coursework submission files. Defaults to <c>App_Data/Submissions</c>.
    /// </summary>
    public string StorageRootPath { get; set; } = "App_Data/Submissions";

    /// <summary>
    /// Maximum allowed file upload size in bytes. Defaults to 25 MB (26,214,400 bytes).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
}
