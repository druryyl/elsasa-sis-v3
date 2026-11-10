namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeacherProfiles;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing filtered and paginated teacher profiles work queue queries (TD-02, TD-07, P2-S07).
/// </summary>
public sealed class GetTeacherProfilesHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeacherProfilesHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    /// <summary>
    /// Executes query returning paginated faculty list categorized by work queue filters.
    /// </summary>
    public async Task<Result<PaginatedList<TeacherSummaryDto>>> HandleAsync(
        string? queueFilter = null,
        string? standing = null,
        string? campus = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (safePage - 1) * safePageSize;

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        var normalizedQueue = queueFilter?.Trim().ToLowerInvariant();
        switch (normalizedQueue)
        {
            case "pending_approval":
                conditions.Add("tp.standing = 'pending_approval'");
                break;
            case "pending_verification":
                conditions.Add("EXISTS (SELECT 1 FROM teaching.qualification_evidence qe WHERE qe.teacher_profile_id = tp.id AND qe.verification_state = 'submitted')");
                break;
            case "pending_authorization":
                conditions.Add("tp.standing = 'active' AND NOT EXISTS (SELECT 1 FROM teaching.teaching_authorizations ta WHERE ta.teacher_profile_id = tp.id AND ta.status = 'active')");
                break;
            case "corrections":
                conditions.Add("EXISTS (SELECT 1 FROM teaching.teaching_record_corrections trc WHERE trc.teacher_profile_id = tp.id AND trc.status = 'requested')");
                break;
            default:
                // "all" or unspecified - no extra queue condition
                break;
        }

        if (!string.IsNullOrWhiteSpace(standing))
        {
            conditions.Add("tp.standing = @Standing");
            parameters.Add("Standing", standing.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(campus) && !campus.Trim().Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("(tp.campus_affiliation = @Campus OR tp.campus_affiliation = 'All')");
            parameters.Add("Campus", campus.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("(tp.teacher_number LIKE @SearchPattern OR tp.full_name LIKE @SearchPattern OR tp.display_name LIKE @SearchPattern OR tp.national_id_or_external_ref LIKE @SearchPattern)");
            parameters.Add("SearchPattern", $"%{search.Trim()}%");
        }

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", safePageSize);

        var whereSql = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var querySql = $"""
            SELECT COUNT(1)
            FROM teaching.teacher_profiles tp
            {whereSql};

            SELECT
                tp.id AS Id,
                tp.teacher_number AS TeacherNumber,
                tp.full_name AS FullName,
                tp.display_name AS DisplayName,
                tp.teaching_role_id AS TeachingRoleId,
                COALESCE(tr.name, N'') AS TeachingRoleName,
                tp.campus_affiliation AS CampusAffiliation,
                tp.department AS Department,
                tp.standing AS Standing,
                tp.email AS Email,
                tp.phone AS Phone,
                CASE 
                    WHEN EXISTS (
                        SELECT 1 
                        FROM teaching.teaching_authorizations ta 
                        WHERE ta.teacher_profile_id = tp.id 
                          AND ta.status = 'active' 
                          AND ta.unverified_credential_warning = 1
                    ) THEN CAST(1 AS BIT) 
                    ELSE CAST(0 AS BIT) 
                END AS HasUnverifiedWarning,
                (
                    SELECT COUNT(1) 
                    FROM teaching.teaching_authorizations ta 
                    WHERE ta.teacher_profile_id = tp.id 
                      AND ta.status = 'active'
                ) AS ActiveAuthorizationsCount
            FROM teaching.teacher_profiles tp
            LEFT JOIN teaching.teaching_roles tr ON tr.id = tp.teaching_role_id
            {whereSql}
            ORDER BY tp.full_name ASC, tp.teacher_number ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(querySql, parameters, cancellationToken: cancellationToken));

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<TeacherSummaryDto>()).AsList();

        return Result.Success(PaginatedList<TeacherSummaryDto>.Create(items, totalCount, safePage, safePageSize));
    }
}
