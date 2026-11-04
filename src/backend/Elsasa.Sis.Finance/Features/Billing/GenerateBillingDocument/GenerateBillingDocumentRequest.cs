namespace Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;

/// <summary>
/// Command payload for compiling open student obligations into an authorized bill/invoice (FIN-WF-04, UC-FIN-03, GAP-005, TD-05).
/// </summary>
/// <param name="StudentId">Target student identity UUID.</param>
/// <param name="ObligationIds">Optional explicit list of obligation UUIDs. If omitted or empty, compiles all open obligations.</param>
/// <param name="PaymentDueDate">Optional explicit payment due date. If omitted, derives from latest included obligation due date.</param>
/// <param name="Notes">Optional administrative or billing notes.</param>
public sealed record GenerateBillingDocumentRequest(
    Guid StudentId,
    IReadOnlyList<Guid>? ObligationIds = null,
    DateOnly? PaymentDueDate = null,
    string? Notes = null);
