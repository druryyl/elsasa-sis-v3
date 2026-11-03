namespace Elsasa.Sis.Students.Features.Admission.GetApplicationStatusByNumber;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying sanitized public admission status by application number and birth date (ADR-004, ADR-005).
/// </summary>
public sealed class GetApplicationStatusByNumberHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectApplicationStatusMultiSql = """
        SELECT
            app.id AS Id,
            app.application_number AS ApplicationNumber,
            a.full_name AS ApplicantFullName,
            app.requested_entry_level AS RequestedEntryLevel,
            app.status AS Status,
            app.registration_date AS RegistrationDate
        FROM students.admission_applications app
        INNER JOIN students.applicants a ON a.id = app.applicant_id
        WHERE app.application_number = @ApplicationNumber
          AND a.date_of_birth = @DateOfBirth;

        SELECT
            d.document_type AS DocumentType,
            d.submission_status AS SubmissionStatus
        FROM students.applicant_documents d
        INNER JOIN students.admission_applications app ON app.id = d.application_id
        INNER JOIN students.applicants a ON a.id = app.applicant_id
        WHERE app.application_number = @ApplicationNumber
          AND a.date_of_birth = @DateOfBirth
        ORDER BY d.created_at ASC;

        SELECT
            dec.decision_type AS DecisionType,
            dec.effective_date AS EffectiveDate
        FROM students.admission_decisions dec
        INNER JOIN students.admission_applications app ON app.id = dec.application_id
        INNER JOIN students.applicants a ON a.id = app.applicant_id
        WHERE app.application_number = @ApplicationNumber
          AND a.date_of_birth = @DateOfBirth;
        """;

    private sealed record PublicApplicationHeaderRow(
        Guid Id,
        string ApplicationNumber,
        string ApplicantFullName,
        string RequestedEntryLevel,
        string Status,
        DateOnly RegistrationDate);

    /// <summary>
    /// Retrieves a sanitized public tracking projection for an application matching the application number and applicant birth date.
    /// </summary>
    public async Task<Result<ApplicationStatusResponse>> HandleAsync(
        string? applicationNumber,
        DateOnly? dateOfBirth,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(applicationNumber) || dateOfBirth is null)
        {
            return Result.Failure<ApplicationStatusResponse>(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    "Application number and date of birth are required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _selectApplicationStatusMultiSql,
            new
            {
                ApplicationNumber = applicationNumber.Trim(),
                DateOfBirth = dateOfBirth.Value
            },
            cancellationToken: cancellationToken));

        var header = await multi.ReadSingleOrDefaultAsync<PublicApplicationHeaderRow>();
        if (header is null)
        {
            return Result.Failure<ApplicationStatusResponse>(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    $"No admission application was found matching number '{applicationNumber.Trim()}' and date of birth '{dateOfBirth.Value:yyyy-MM-dd}'."));
        }

        var documentStatuses = (await multi.ReadAsync<PublicDocumentStatusItem>()).AsList();
        var decisionNotice = await multi.ReadSingleOrDefaultAsync<PublicDecisionNotice>();

        var response = new ApplicationStatusResponse(
            header.ApplicationNumber,
            header.ApplicantFullName,
            header.RequestedEntryLevel,
            header.Status,
            header.RegistrationDate,
            documentStatuses,
            decisionNotice);

        return Result.Success(response);
    }
}
