namespace Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;

/// <summary>
/// Projectable open obligation DTO returned as part of student financial account dossier.
/// </summary>
public sealed record OpenObligationDto(
    Guid Id,
    string ObligationNumber,
    Guid StudentAccountId,
    Guid StudentId,
    Guid? ChargeRuleId,
    Guid PolicyId,
    string Description,
    string ChargeType,
    bool IsMandatory,
    long OriginalAmount,
    string CurrencyCode,
    DateOnly IssueDate,
    DateOnly DueDate,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Status,
    string AuthorizedBy,
    DateTimeOffset CreatedAt);
