namespace Elsasa.Sis.Finance.Services;

/// <summary>
/// Thread-safe and query-driven sequential payer number generator.
/// Formats payer numbers as PYR-{Year}-{Sequence:D4}.
/// </summary>
public static class PayerNumberGenerator
{
    public const string Prefix = "PYR";

    /// <summary>
    /// Formats a payer number according to the canonical convention: PYR-{Year}-{Sequence:D4}.
    /// </summary>
    /// <param name="sequence">Positive 1-based sequential number.</param>
    /// <param name="year">Optional calendar year (defaults to UtcNow.Year).</param>
    /// <returns>Formatted canonical payer number.</returns>
    public static string Format(int sequence, int? year = null)
    {
        var y = year ?? DateTime.UtcNow.Year;
        return $"{Prefix}-{y}-{sequence:D4}";
    }
}
