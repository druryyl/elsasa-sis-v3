namespace Elsasa.Sis.Finance.Services;

/// <summary>
/// Helper converting numeric monetary values into Indonesian words (Terbilang) for official receipt vouchers.
/// </summary>
public static class NumberToWordsConverter
{
    private static readonly string[] _units =
    [
        "", "Satu", "Dua", "Tiga", "Empat", "Lima", "Enam", "Tujuh", "Delapan", "Sembilan",
        "Sepuluh", "Sebelas"
    ];

    /// <summary>
    /// Converts a monetary integer into formal Indonesian verbal words appended with 'Rupiah'.
    /// </summary>
    /// <param name="amount">The numeric gross monetary value.</param>
    /// <returns>Verbal Indonesian representation (e.g., 'Satu Juta Lima Ratus Ribu Rupiah').</returns>
    public static string ToIndonesianWords(long amount)
    {
        if (amount == 0)
        {
            return "Nol Rupiah";
        }

        if (amount < 0)
        {
            return $"Minus {ConvertNumber(-amount)} Rupiah".Trim();
        }

        return $"{ConvertNumber(amount)} Rupiah".Trim();
    }

    private static string ConvertNumber(long number)
    {
        if (number == 0)
        {
            return string.Empty;
        }

        if (number < 12)
        {
            return _units[number];
        }

        if (number < 20)
        {
            return $"{_units[number - 10]} Belas";
        }

        if (number < 100)
        {
            return $"{_units[number / 10]} Puluh {ConvertNumber(number % 10)}".Trim();
        }

        if (number < 200)
        {
            return $"Seratus {ConvertNumber(number - 100)}".Trim();
        }

        if (number < 1000)
        {
            return $"{_units[number / 100]} Ratus {ConvertNumber(number % 100)}".Trim();
        }

        if (number < 2000)
        {
            return $"Seribu {ConvertNumber(number - 1000)}".Trim();
        }

        if (number < 1_000_000)
        {
            return $"{ConvertNumber(number / 1000)} Ribu {ConvertNumber(number % 1000)}".Trim();
        }

        if (number < 1_000_000_000)
        {
            return $"{ConvertNumber(number / 1_000_000)} Juta {ConvertNumber(number % 1_000_000)}".Trim();
        }

        if (number < 1_000_000_000_000)
        {
            return $"{ConvertNumber(number / 1_000_000_000)} Miliar {ConvertNumber(number % 1_000_000_000)}".Trim();
        }

        return $"{ConvertNumber(number / 1_000_000_000_000)} Triliun {ConvertNumber(number % 1_000_000_000_000)}".Trim();
    }
}
