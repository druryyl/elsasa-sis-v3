namespace Elsasa.Sis.Teaching.Features.RecordCorrections;

/// <summary>
/// Metadata and canonical column mapping for auditable record corrections on finalized faculty records (TD-06, P5-S14).
/// Provides strict whitelisting to safeguard against SQL injection and data corruption.
/// </summary>
internal static class RecordCorrectionColumnHelper
{
    internal sealed record ColumnMetadata(string CanonicalColumnName, bool IsDate, bool IsNullable);

    private static readonly Dictionary<string, ColumnMetadata> _teacherProfilesColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["teacher_number"] = new("teacher_number", false, false),
        ["teachernumber"] = new("teacher_number", false, false),
        ["full_name"] = new("full_name", false, false),
        ["fullname"] = new("full_name", false, false),
        ["display_name"] = new("display_name", false, false),
        ["displayname"] = new("display_name", false, false),
        ["employee_ref"] = new("employee_ref", false, true),
        ["employeeref"] = new("employee_ref", false, true),
        ["national_id_or_external_ref"] = new("national_id_or_external_ref", false, true),
        ["nationalidorexternalref"] = new("national_id_or_external_ref", false, true),
        ["email"] = new("email", false, true),
        ["phone"] = new("phone", false, true),
        ["campus_affiliation"] = new("campus_affiliation", false, false),
        ["campusaffiliation"] = new("campus_affiliation", false, false),
        ["department"] = new("department", false, true),
        ["standing"] = new("standing", false, false),
        ["effective_from"] = new("effective_from", true, false),
        ["effectivefrom"] = new("effective_from", true, false),
        ["effective_to"] = new("effective_to", true, true),
        ["effectiveto"] = new("effective_to", true, true),
        ["standing_reason"] = new("standing_reason", false, true),
        ["standingreason"] = new("standing_reason", false, true),
        ["notes"] = new("notes", false, true)
    };

    private static readonly Dictionary<string, ColumnMetadata> _authorizationsColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["authorization_number"] = new("authorization_number", false, false),
        ["authorizationnumber"] = new("authorization_number", false, false),
        ["scope_description"] = new("scope_description", false, false),
        ["scopedescription"] = new("scope_description", false, false),
        ["campus_scope"] = new("campus_scope", false, false),
        ["campusscope"] = new("campus_scope", false, false),
        ["program_scope"] = new("program_scope", false, true),
        ["programscope"] = new("program_scope", false, true),
        ["level_scope"] = new("level_scope", false, true),
        ["levelscope"] = new("level_scope", false, true),
        ["delivery_mode_scope"] = new("delivery_mode_scope", false, true),
        ["deliverymodescope"] = new("delivery_mode_scope", false, true),
        ["status"] = new("status", false, false),
        ["effective_start_date"] = new("effective_start_date", true, false),
        ["effectivestartdate"] = new("effective_start_date", true, false),
        ["effective_end_date"] = new("effective_end_date", true, true),
        ["effectiveenddate"] = new("effective_end_date", true, true),
        ["conditions_or_restrictions"] = new("conditions_or_restrictions", false, true),
        ["conditionsorrestrictions"] = new("conditions_or_restrictions", false, true),
        ["decision_authority"] = new("decision_authority", false, false),
        ["decisionauthority"] = new("decision_authority", false, false),
        ["suspension_or_revocation_reason"] = new("suspension_or_revocation_reason", false, true),
        ["suspensionorrevocationreason"] = new("suspension_or_revocation_reason", false, true),
        ["notes"] = new("notes", false, true)
    };

    public static bool TryNormalizeTargetTable(string? rawTable, out string canonicalTable)
    {
        canonicalTable = string.Empty;
        if (string.IsNullOrWhiteSpace(rawTable))
        {
            return false;
        }

        var trimmed = rawTable.Trim();
        if (trimmed.StartsWith("teaching.", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["teaching.".Length..];
        }

        if (string.Equals(trimmed, "teacher_profiles", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "teacherprofiles", StringComparison.OrdinalIgnoreCase))
        {
            canonicalTable = "teacher_profiles";
            return true;
        }

        if (string.Equals(trimmed, "teaching_authorizations", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "teachingauthorizations", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "authorizations", StringComparison.OrdinalIgnoreCase))
        {
            canonicalTable = "teaching_authorizations";
            return true;
        }

        return false;
    }

    public static bool TryGetColumnMetadata(string canonicalTable, string? rawFieldName, out ColumnMetadata metadata)
    {
        metadata = default!;
        if (string.IsNullOrWhiteSpace(rawFieldName))
        {
            return false;
        }

        var trimmed = rawFieldName.Trim();
        if (string.Equals(canonicalTable, "teacher_profiles", StringComparison.OrdinalIgnoreCase))
        {
            return _teacherProfilesColumns.TryGetValue(trimmed, out metadata!);
        }

        if (string.Equals(canonicalTable, "teaching_authorizations", StringComparison.OrdinalIgnoreCase))
        {
            return _authorizationsColumns.TryGetValue(trimmed, out metadata!);
        }

        return false;
    }
}
