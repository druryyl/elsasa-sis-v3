namespace Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationsList;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying the filterable admission application queue (ADR-004, ADR-005).
/// </summary>
public sealed class GetAdmissionApplicationsListHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectQueueSql = """
        SELECT
            app.id AS Id,
            app.application_number AS ApplicationNumber,
            a.full_name AS ApplicantName,
            a.government_id_number AS GovernmentIdNumber,
            app.admission_period_id AS AdmissionPeriodId,
            app.requested_entry_level AS RequestedEntryLevel,
            app.status AS Status,
            app.registration_date AS RegistrationDate
        FROM students.admission_applications app
        INNER JOIN students.applicants a ON a.id = app.applicant_id
        WHERE (@AdmissionPeriodId IS NULL OR app.admission_period_id = @AdmissionPeriodId)
          AND (@Status IS NULL OR app.status = @Status)
          AND (@RequestedEntryLevel IS NULL OR app.requested_entry_level = @RequestedEntryLevel)
          AND (@SearchPattern IS NULL OR (
                app.application_number LIKE @SearchPattern
                OR a.full_name LIKE @SearchPattern
                OR a.government_id_number LIKE @SearchPattern))
        ORDER BY app.registration_date DESC, app.created_at DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    /// <summary>
    /// Retrieves paginated admission applications matching optional filters.
    /// </summary>
    public async Task<Result<IReadOnlyList<AdmissionApplicationListItemResponse>>> HandleAsync(
        Guid? admissionPeriodId,
        string? status,
        string? requestedEntryLevel,
        string? searchTerm,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectivePage = page < 1 ? 1 : page;
        var effectivePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (effectivePage - 1) * effectivePageSize;
        var searchPattern = string.IsNullOrWhiteSpace(searchTerm) ? null : $"%{searchTerm.Trim()}%";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var items = await connection.QueryAsync<AdmissionApplicationListItemResponse>(new CommandDefinition(
            _selectQueueSql,
            new
            {
                AdmissionPeriodId = admissionPeriodId,
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                RequestedEntryLevel = string.IsNullOrWhiteSpace(requestedEntryLevel) ? null : requestedEntryLevel.Trim(),
                SearchPattern = searchPattern,
                Offset = offset,
                PageSize = effectivePageSize
            },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AdmissionApplicationListItemResponse>>(items.AsList());
    }
}
