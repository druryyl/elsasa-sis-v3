namespace Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;

/// <summary>
/// Projectable billing document summary returned in student financial account dossier.
/// </summary>
public sealed record RecentBillingDocumentDto(
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
    DateTimeOffset CreatedAt);
