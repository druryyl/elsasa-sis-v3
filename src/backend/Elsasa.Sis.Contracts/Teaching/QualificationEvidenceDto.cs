namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing qualification documentary evidence supporting a teacher profile.
/// </summary>
/// <param name="Id">The unique identifier of the qualification evidence record.</param>
/// <param name="TeacherProfileId">The unique identifier of the owning teacher profile.</param>
/// <param name="QualificationTypeId">The unique identifier of the recognized qualification type.</param>
/// <param name="Title">The descriptive title of the credential.</param>
/// <param name="IssuingAuthority">The authority or institution that issued the qualification.</param>
/// <param name="CredentialIdentifier">Optional credential registration or serial number.</param>
/// <param name="IssueDate">The date when the credential was issued.</param>
/// <param name="ExpiryDate">Optional date when credential validity expires.</param>
/// <param name="DocumentReference">Optional secure document archive reference or link.</param>
/// <param name="VerificationState">The verification lifecycle state (claimed, submitted, verified, rejected, expired, revoked).</param>
/// <param name="VerifiedBy">The identifier or name of the authority who verified the credential.</param>
/// <param name="VerifiedDate">Optional date when verification was completed.</param>
/// <param name="VerificationNotes">Optional verification notes or comments.</param>
/// <param name="RejectionReason">Optional explanation if the evidence was rejected.</param>
/// <param name="RestrictionsOrConditions">Optional restrictions attached to the credential verification.</param>
public sealed record QualificationEvidenceDto(
    Guid Id,
    Guid TeacherProfileId,
    Guid QualificationTypeId,
    string Title,
    string IssuingAuthority,
    string? CredentialIdentifier,
    DateOnly IssueDate,
    DateOnly? ExpiryDate,
    string? DocumentReference,
    string VerificationState,
    string? VerifiedBy,
    DateOnly? VerifiedDate,
    string? VerificationNotes,
    string? RejectionReason,
    string? RestrictionsOrConditions);
