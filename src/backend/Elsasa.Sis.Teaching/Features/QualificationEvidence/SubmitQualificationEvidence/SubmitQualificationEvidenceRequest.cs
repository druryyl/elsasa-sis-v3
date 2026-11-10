namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.SubmitQualificationEvidence;

/// <summary>
/// Request record for submitting qualification documentary evidence for a teacher profile (TD-05, P3-S09).
/// </summary>
/// <param name="QualificationTypeId">The recognized qualification type identifier.</param>
/// <param name="Title">The descriptive title of the credential.</param>
/// <param name="IssuingAuthority">The authority or institution that issued the qualification.</param>
/// <param name="CredentialIdentifier">Optional credential registration or serial number.</param>
/// <param name="IssueDate">The date when the credential was issued.</param>
/// <param name="ExpiryDate">Optional date when credential validity expires.</param>
/// <param name="DocumentReference">Optional secure document archive reference or link.</param>
/// <param name="RestrictionsOrConditions">Optional restrictions attached to the credential.</param>
public sealed record SubmitQualificationEvidenceRequest(
    Guid QualificationTypeId,
    string Title,
    string IssuingAuthority,
    string? CredentialIdentifier,
    DateOnly IssueDate,
    DateOnly? ExpiryDate,
    string? DocumentReference,
    string? RestrictionsOrConditions = null);
