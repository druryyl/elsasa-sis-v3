namespace Elsasa.Sis.Finance.Features.Payers.CreatePayer;

/// <summary>
/// Command payload for explicitly creating a new payer entity (SCR-FIN-02, TD-03).
/// </summary>
/// <param name="Name">Full legal name or company moniker.</param>
/// <param name="Type">Classification (e.g. 'parent_guardian', 'sponsor', 'embassy', 'foundation', 'other'). Defaults to 'parent_guardian' if not provided.</param>
/// <param name="IdentificationNumber">Optional NIK, NPWP, or corporate registration number.</param>
/// <param name="Email">Optional contact email address.</param>
/// <param name="Phone">Optional contact telephone/mobile number.</param>
/// <param name="BillingAddress">Optional physical or postal billing address.</param>
/// <param name="BankAccountName">Optional account holder name for refund routing.</param>
/// <param name="BankAccountNumber">Optional bank account number.</param>
/// <param name="BankName">Optional banking institution moniker (e.g. 'BCA', 'Mandiri').</param>
/// <param name="Notes">Optional administrative notes or cashier comments.</param>
public sealed record CreatePayerRequest(
    string Name,
    string? Type = null,
    string? IdentificationNumber = null,
    string? Email = null,
    string? Phone = null,
    string? BillingAddress = null,
    string? BankAccountName = null,
    string? BankAccountNumber = null,
    string? BankName = null,
    string? Notes = null)
{
    /// <summary>
    /// Alias for Type matching property naming variations.
    /// </summary>
    public string? PayerType => Type;

    /// <summary>
    /// Alias for Phone matching property naming variations.
    /// </summary>
    public string? PhoneNumber => Phone;
}
