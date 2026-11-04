namespace Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;

/// <summary>
/// Frozen itemized line item inside generated billing document.
/// </summary>
public sealed record GenerateBillingDocumentItemResponse(
    Guid Id,
    int LineSequence,
    string ItemType,
    string ReferenceEntityType,
    Guid ReferenceEntityId,
    string Description,
    long Amount,
    string CurrencyCode);

/// <summary>
/// Summary response returned upon compiling a billing document (bill).
/// </summary>
public sealed record GenerateBillingDocumentResponse(
    Guid Id,
    string DocumentNumber,
    Guid StudentAccountId,
    Guid StudentId,
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
    IReadOnlyList<GenerateBillingDocumentItemResponse> Items,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
