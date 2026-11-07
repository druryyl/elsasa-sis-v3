namespace Elsasa.Sis.Api.Features.DemoData;

/// <summary>
/// Configuration options for the Demo Data subsystem (ADR-001, ADR-006, OQ-004).
/// Controls whether demo data seeding is enabled and the default password for demo accounts.
/// </summary>
public sealed class DemoDataOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "DemoData";

    /// <summary>
    /// Gets or sets a value indicating whether demo data seeding is enabled.
    /// Defaults to true for Development and Testing environments.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default password for all pre-configured demo user accounts.
    /// </summary>
    public string DefaultPassword { get; set; } = "Demo123!";
}