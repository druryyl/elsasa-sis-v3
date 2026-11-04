namespace Elsasa.Sis.Finance.Features.Billing.GetBillingDocumentById;

/// <summary>
/// Detailed frozen line item representation inside an invoice or statement.
/// </summary>
public sealed record BillingDocumentDetailItemDto(
    Guid Id,
    int LineSequence,
    string ItemType,
    string ReferenceEntityType,
    Guid ReferenceEntityId,
    string Description,
    long Amount,
    string CurrencyCode,
    DateTimeOffset CreatedAt);

/// <summary>
/// Detailed billing document snapshot response including recipient contact, payment due date, total amount, and frozen itemized line items list (FIN-WF-04, UC-FIN-03, GAP-005, TD-05).
/// </summary>
public sealed record BillingDocumentDetailResponse(
    Guid Id,
    string DocumentNumber,
    Guid StudentAccountId,
    Guid StudentId,
    string StudentNumber,
    string StudentFullName,
    string DocumentType,
    string RecipientName,
    string RecipientRelationship,
    string RecipientContact,
    DateOnly? BillingPeriodStart,
    DateOnly? BillingPeriodEnd,
    DateOnly IssueDate,
    DateOnly? PaymentDueDate,
    long TotalBilledAmount,
    string CurrencyCode,
    string DeliveryChannel,
    string DeliveryStatus,
    DateTimeOffset? SentAt,
    string? Notes,
    IReadOnlyList<BillingDocumentDetailItemDto> Items,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
