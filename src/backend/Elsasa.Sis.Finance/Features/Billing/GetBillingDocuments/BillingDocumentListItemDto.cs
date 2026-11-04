namespace Elsasa.Sis.Finance.Features.Billing.GetBillingDocuments;

/// <summary>
/// Projection DTO for billing document summary items in listing queries.
/// </summary>
public sealed record BillingDocumentListItemDto(
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
    DateTimeOffset CreatedAt);
