namespace Elsasa.Sis.Finance.Features.Billing.GenerateStatement;

/// <summary>
/// Frozen itemized line item inside generated financial statement.
/// </summary>
public sealed record GenerateStatementItemResponse(
    Guid Id,
    int LineSequence,
    string ItemType,
    string ReferenceEntityType,
    Guid ReferenceEntityId,
    string Description,
    long Amount,
    string CurrencyCode);

/// <summary>
/// Summary response returned upon compiling a periodic financial statement.
/// </summary>
public sealed record GenerateStatementResponse(
    Guid Id,
    string DocumentNumber,
    Guid StudentAccountId,
    Guid StudentId,
    string DocumentType,
    string RecipientName,
    string RecipientRelationship,
    string RecipientContact,
    DateOnly BillingPeriodStart,
    DateOnly BillingPeriodEnd,
    DateOnly IssueDate,
    DateOnly? PaymentDueDate,
    long TotalBilledAmount,
    string CurrencyCode,
    string DeliveryChannel,
    string DeliveryStatus,
    DateTimeOffset? SentAt,
    string? Notes,
    IReadOnlyList<GenerateStatementItemResponse> Items,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
