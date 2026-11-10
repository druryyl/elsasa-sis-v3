namespace Elsasa.Sis.Teaching.Features.RecordCorrections.GetRecordCorrections;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying chronological audit trail of faculty record corrections (TD-06, P5-S14).
/// </summary>
public sealed class GetRecordCorrectionsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetRecordCorrectionsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<IReadOnlyList<TeachingRecordCorrectionDto>>> HandleAsync(
        Guid? teacherProfileId,
        string? targetTableName,
        string? status,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (teacherProfileId.HasValue && teacherProfileId.Value != Guid.Empty)
        {
            conditions.Add("trc.teacher_profile_id = @TeacherProfileId");
            parameters.Add("TeacherProfileId", teacherProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(targetTableName))
        {
            var raw = targetTableName.Trim();
            if (RecordCorrectionColumnHelper.TryNormalizeTargetTable(raw, out var canonicalTable))
            {
                conditions.Add("(trc.target_table_name = @Table OR trc.target_table_name = @TableWithPrefix)");
                parameters.Add("Table", canonicalTable);
                parameters.Add("TableWithPrefix", "teaching." + canonicalTable);
            }
            else
            {
                conditions.Add("trc.target_table_name = @TargetTable");
                parameters.Add("TargetTable", raw);
            }
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("trc.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        var sql = $"""
            SELECT
                trc.id AS Id,
                trc.correction_number AS CorrectionNumber,
                trc.target_table_name AS TargetTableName,
                trc.target_record_id AS TargetRecordId,
                trc.teacher_profile_id AS TeacherProfileId,
                trc.field_name AS FieldName,
                trc.original_value AS OriginalValue,
                trc.corrected_value AS CorrectedValue,
                trc.reason_for_correction AS ReasonForCorrection,
                trc.supporting_evidence AS SupportingEvidence,
                trc.downstream_impact_reviewed AS DownstreamImpactReviewed,
                trc.effective_date_implications AS EffectiveDateImplications,
                trc.requested_by AS RequestedBy,
                trc.requested_date AS RequestedDate,
                trc.approved_by AS ApprovedBy,
                trc.approved_date AS ApprovedDate,
                trc.status AS Status
            FROM teaching.teaching_record_corrections trc
            {whereClause}
            ORDER BY trc.requested_date DESC, trc.created_at DESC;
            """;

        var rows = (await connection.QueryAsync<TeachingRecordCorrectionDto>(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken))).AsList();

        return Result.Success<IReadOnlyList<TeachingRecordCorrectionDto>>(rows);
    }
}
