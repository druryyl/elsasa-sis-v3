namespace Elsasa.Sis.Students.Features.Admission.Common;

using System.Globalization;

/// <summary>
/// Formats sequential admission application numbers according to ADR-005 and the Stage 1 Admission specification:
/// ADM-{Year}-{Seq:D4} (e.g., ADM-2026-0001).
/// </summary>
public static class ApplicationNumberFormatter
{
    private const string _prefix = "ADM";

    /// <summary>
    /// Formats an application number using the specified 4-digit academic or calendar year and sequence value.
    /// </summary>
    /// <param name="year">The 4-digit year component.</param>
    /// <param name="sequence">The monotonically increasing sequence value.</param>
    /// <returns>A formatted traceable application number string (e.g. "ADM-2026-0001").</returns>
    public static string Format(int year, int sequence)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{_prefix}-{year:D4}-{sequence:D4}");
    }

    /// <summary>
    /// Formats an application number using the specified year code string and sequence value.
    /// </summary>
    /// <param name="yearCode">The year code or year string (e.g. "2026").</param>
    /// <param name="sequence">The monotonically increasing sequence value.</param>
    /// <returns>A formatted traceable application number string (e.g. "ADM-2026-0001").</returns>
    public static string Format(string yearCode, int sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yearCode);
        return string.Create(CultureInfo.InvariantCulture, $"{_prefix}-{yearCode.Trim()}-{sequence:D4}");
    }
}
