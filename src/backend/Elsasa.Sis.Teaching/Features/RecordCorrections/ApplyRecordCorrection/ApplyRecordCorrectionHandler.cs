namespace Elsasa.Sis.Teaching.Features.RecordCorrections.ApplyRecordCorrection;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler for role-aware auditable record corrections on finalized teaching records (TD-06, P5-S14).
/// Teaching Authority / Admin directly applies corrections in a single atomic transaction; Teaching Coordinator submits requested corrections.
/// </summary>
public sealed class ApplyRecordCorrectionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ApplyRecordCorrectionHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _insertCorrectionSql = """
        INSERT INTO teaching.teaching_record_corrections (
            id,
            correction_number,
            target_table_name,
            target_record_id,
            teacher_profile_id,
            field_name,
            original_value,
            corrected_value,
            reason_for_correction,
            supporting_evidence,
            downstream_impact_reviewed,
            effective_date_implications,
            requested_by,
            requested_date,
            approved_by,
            approved_date,
            status,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @CorrectionNumber,
            @TargetTableName,
            @TargetRecordId,
            @TeacherProfileId,
            @FieldName,
            @OriginalValue,
            @CorrectedValue,
            @ReasonForCorrection,
            @SupportingEvidence,
            @DownstreamImpactReviewed,
            @EffectiveDateImplications,
            @RequestedBy,
            @RequestedDate,
            @ApprovedBy,
            @ApprovedDate,
            @Status,
            SYSUTCDATETIME(),
            @RequestedBy
        );
        """;

    public async Task<Result<TeachingRecordCorrectionDto>> HandleAsync(
        ApplyRecordCorrectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!RecordCorrectionColumnHelper.TryNormalizeTargetTable(request.TargetTableName, out var canonicalTable))
        {
            return Result.Failure<TeachingRecordCorrectionDto>(DomainError.Validation(
                "Teaching.InvalidTargetTable",
                $"Target table '{request.TargetTableName}' is invalid. Allowed tables are 'teacher_profiles' and 'teaching_authorizations'."));
        }

        if (!RecordCorrectionColumnHelper.TryGetColumnMetadata(canonicalTable, request.FieldName, out var columnMetadata))
        {
            return Result.Failure<TeachingRecordCorrectionDto>(DomainError.Validation(
                "Teaching.InvalidTargetField",
                $"Field '{request.FieldName}' is not eligible for record correction on table '{canonicalTable}'."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify target record exists, retrieve teacher_profile_id, and capture current original_value
        Guid? teacherProfileId;
        object? rawOriginal;

        if (string.Equals(canonicalTable, "teacher_profiles", StringComparison.OrdinalIgnoreCase))
        {
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT TOP 1 1 FROM teaching.teacher_profiles WHERE id = @Id;",
                new { Id = request.TargetRecordId },
                cancellationToken: cancellationToken));

            if (!exists)
            {
                return Result.Failure<TeachingRecordCorrectionDto>(DomainError.NotFound(
                    "Teaching.TargetRecordNotFound",
                    $"Teacher profile with ID '{request.TargetRecordId}' was not found."));
            }

            teacherProfileId = request.TeacherProfileId ?? request.TargetRecordId;

            var selectColumnSql = $"SELECT [{columnMetadata.CanonicalColumnName}] FROM teaching.teacher_profiles WHERE id = @Id;";
            rawOriginal = await connection.ExecuteScalarAsync<object?>(new CommandDefinition(
                selectColumnSql,
                new { Id = request.TargetRecordId },
                cancellationToken: cancellationToken));
        }
        else
        {
            var authTeacherId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT teacher_profile_id FROM teaching.teaching_authorizations WHERE id = @Id;",
                new { Id = request.TargetRecordId },
                cancellationToken: cancellationToken));

            if (authTeacherId is null)
            {
                return Result.Failure<TeachingRecordCorrectionDto>(DomainError.NotFound(
                    "Teaching.TargetRecordNotFound",
                    $"Teaching authorization with ID '{request.TargetRecordId}' was not found."));
            }

            teacherProfileId = request.TeacherProfileId ?? authTeacherId.Value;

            var selectColumnSql = $"SELECT [{columnMetadata.CanonicalColumnName}] FROM teaching.teaching_authorizations WHERE id = @Id;";
            rawOriginal = await connection.ExecuteScalarAsync<object?>(new CommandDefinition(
                selectColumnSql,
                new { Id = request.TargetRecordId },
                cancellationToken: cancellationToken));
        }

        var originalValue = rawOriginal switch
        {
            null or DBNull => string.Empty,
            DateTime dt => dt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => Convert.ToString(rawOriginal, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };

        // 2. Generate unique correction number (CORR-YYYY-XXXX)
        var year = DateTime.UtcNow.Year;
        var prefix = $"CORR-{year}-";
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM teaching.teaching_record_corrections WHERE correction_number LIKE @Prefix;",
            new { Prefix = prefix + "%" },
            cancellationToken: cancellationToken));

        var seq = count + 1;
        string correctionNumber;
        while (true)
        {
            var candidate = $"{prefix}{seq:D4}";
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT TOP 1 1 FROM teaching.teaching_record_corrections WHERE correction_number = @Candidate;",
                new { Candidate = candidate },
                cancellationToken: cancellationToken));

            if (!exists)
            {
                correctionNumber = candidate;
                break;
            }

            seq++;
        }

        // 3. Role-Aware Branching (TD-06)
        var isAuthorityOrAdmin = IsTeachingAuthorityOrAdmin();
        var correctionId = Guid.NewGuid();
        var auditUser = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayDt = today.ToDateTime(TimeOnly.MinValue);
        var downstreamImpactReviewed = request.DownstreamImpactReviewed ?? true;

        string status;
        string? approvedBy;
        DateOnly? approvedDate;

        if (isAuthorityOrAdmin)
        {
            status = "applied";
            approvedBy = auditUser;
            approvedDate = today;
        }
        else
        {
            status = "requested";
            approvedBy = null;
            approvedDate = null;
        }

        // Prepare parameter value for SQL column update if applying directly
        object? sqlTargetValue = request.CorrectedValue;
        if (columnMetadata.IsDate)
        {
            if (string.IsNullOrWhiteSpace(request.CorrectedValue) || string.Equals(request.CorrectedValue.Trim(), "null", StringComparison.OrdinalIgnoreCase))
            {
                sqlTargetValue = DBNull.Value;
            }
            else if (DateOnly.TryParse(request.CorrectedValue.Trim(), out var parsedDate))
            {
                sqlTargetValue = parsedDate.ToDateTime(TimeOnly.MinValue);
            }
        }
        else if (columnMetadata.IsNullable && (string.Equals(request.CorrectedValue.Trim(), "null", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(request.CorrectedValue)))
        {
            sqlTargetValue = DBNull.Value;
        }

        // 4. Atomic Execution
        if (connection is SqlConnection sqlConn && sqlConn.State != System.Data.ConnectionState.Open)
        {
            await sqlConn.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertCorrectionSql,
                new
                {
                    Id = correctionId,
                    CorrectionNumber = correctionNumber,
                    TargetTableName = canonicalTable,
                    TargetRecordId = request.TargetRecordId,
                    TeacherProfileId = teacherProfileId,
                    FieldName = columnMetadata.CanonicalColumnName,
                    OriginalValue = originalValue,
                    CorrectedValue = request.CorrectedValue,
                    ReasonForCorrection = request.ReasonForCorrection.Trim(),
                    SupportingEvidence = string.IsNullOrWhiteSpace(request.SupportingEvidence) ? null : request.SupportingEvidence.Trim(),
                    DownstreamImpactReviewed = downstreamImpactReviewed,
                    EffectiveDateImplications = string.IsNullOrWhiteSpace(request.EffectiveDateImplications) ? null : request.EffectiveDateImplications.Trim(),
                    RequestedBy = auditUser,
                    RequestedDate = todayDt,
                    ApprovedBy = approvedBy,
                    ApprovedDate = approvedDate.HasValue ? todayDt : (object?)null,
                    Status = status
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (isAuthorityOrAdmin)
            {
                var updateSql = $"""
                    UPDATE teaching.{canonicalTable}
                    SET
                        [{columnMetadata.CanonicalColumnName}] = @Val,
                        updated_at = SYSUTCDATETIME(),
                        updated_by = @ApprovedBy
                    WHERE id = @TargetRecordId;
                    """;

                await connection.ExecuteAsync(new CommandDefinition(
                    updateSql,
                    new
                    {
                        Val = sqlTargetValue,
                        ApprovedBy = auditUser,
                        TargetRecordId = request.TargetRecordId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // 5. Post-Commit Domain Event if applied
        if (isAuthorityOrAdmin)
        {
            var domainEvent = new TeachingRecordCorrectedEvent(
                correctionId,
                correctionNumber,
                canonicalTable,
                request.TargetRecordId,
                columnMetadata.CanonicalColumnName,
                auditUser);

            await publisher.Publish(domainEvent, cancellationToken);
        }

        var dto = new TeachingRecordCorrectionDto(
            correctionId,
            correctionNumber,
            canonicalTable,
            request.TargetRecordId,
            teacherProfileId,
            columnMetadata.CanonicalColumnName,
            originalValue,
            request.CorrectedValue,
            request.ReasonForCorrection.Trim(),
            string.IsNullOrWhiteSpace(request.SupportingEvidence) ? null : request.SupportingEvidence.Trim(),
            downstreamImpactReviewed,
            string.IsNullOrWhiteSpace(request.EffectiveDateImplications) ? null : request.EffectiveDateImplications.Trim(),
            auditUser,
            today,
            approvedBy,
            approvedDate,
            status);

        return Result.Success(dto);
    }

    private bool IsTeachingAuthorityOrAdmin()
    {
        if (currentUser.Roles.Any(r =>
            string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "TeachingAuthority", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Teaching Authority", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "KepalaSekolah", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Kepala Sekolah", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "VPAcademics", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "VP Academics", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (currentUser.Permissions.Any(p =>
            string.Equals(p, Permissions.Teaching.Faculty.Authorize, StringComparison.OrdinalIgnoreCase) ||
            p == "*"))
        {
            return true;
        }

        return false;
    }
}
