namespace Elsasa.Sis.Finance.Features.Payers.GetPayers;

/// <summary>
/// Summary DTO for a payer entity within paginated listing.
/// </summary>
/// <param name="Id">The unique identifier of the payer.</param>
/// <param name="PayerNumber">Canonical sequential payer code (e.g. PYR-2026-0001).</param>
/// <param name="PayerName">Full legal name or corporate moniker.</param>
/// <param name="PayerType">Classification (parent_guardian, sponsor, embassy, foundation, etc.).</param>
/// <param name="IdentificationNumber">Optional NIK, NPWP, or entity registration code.</param>
/// <param name="Email">Optional contact email address.</param>
/// <param name="PhoneNumber">Optional contact phone number.</param>
/// <param name="CreatedAt">Timestamp when this payer record was created.</param>
public sealed record PayerListItemDto(
    Guid Id,
    string PayerNumber,
    string PayerName,
    string PayerType,
    string? IdentificationNumber,
    string? Email,
    string? PhoneNumber,
    DateTimeOffset CreatedAt);
