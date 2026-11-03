namespace Elsasa.Sis.Students.Features.Records.Guardians;

/// <summary>
/// Authoritative relationship types and validation helpers for student-guardian relationships.
/// </summary>
public static class GuardianRelationshipTypes
{
    public const string Mother = "mother";
    public const string Father = "father";
    public const string LegalGuardian = "legal_guardian";
    public const string Grandparent = "grandparent";
    public const string Sibling = "sibling";
    public const string Other = "other";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        Mother,
        Father,
        LegalGuardian,
        "legal guardian",
        Grandparent,
        Sibling,
        Other
    };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Allowed.Contains(value.Trim());

    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        if (string.Equals(trimmed, "legal guardian", StringComparison.OrdinalIgnoreCase))
        {
            return LegalGuardian;
        }

        return trimmed.ToLowerInvariant();
    }
}
