namespace Elsasa.Sis.Finance.Services;

/// <summary>
/// Domain service to auto-match and provision payer identities across identification number (NIK/NPWP),
/// contact email, or phone number, eliminating cashier friction and preventing duplication (GAP-005, OQ-002, TD-03).
/// </summary>
public interface IPayerResolver
{
    /// <summary>
    /// Resolves an existing payer ID by searching sequentially for exact matches on identification number,
    /// email, or phone number. If no match is found, auto-generates a new payer record and returns its ID.
    /// </summary>
    /// <param name="payerName">The legal or display name of the payer.</param>
    /// <param name="identificationNumber">Government or tax ID (NIK, NPWP, corporate registration).</param>
    /// <param name="email">Primary contact email address.</param>
    /// <param name="phone">Primary contact phone number.</param>
    /// <param name="payerType">Payer classification (defaults to 'parent_guardian' if null or whitespace).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The unique identifier of the resolved or newly provisioned payer.</returns>
    Task<Guid> ResolveOrCreatePayerAsync(
        string payerName,
        string? identificationNumber,
        string? email,
        string? phone,
        string? payerType,
        CancellationToken ct = default);
}
