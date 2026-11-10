namespace Elsasa.Sis.Teaching.Features.RecordCorrections.ApproveRecordCorrection;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler for Teaching Authority approval of requested record corrections (TD-06, P5-S14).
/// Atomically applies the corrected value to the target table and transitions correction status to 'approved'.
/// </summary>
public sealed class ApproveRecordCorrectionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ApproveRecordCorrectionHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectCorrectionSql = """
        SELECT
            id AS Id,
            correction_number AS CorrectionNumber,
            target_table_name AS TargetTableName,
            target_record_id AS TargetRecordId,
            teacher_profile_id AS TeacherProfileId,
            field_name AS FieldName,
            original_value AS OriginalValue,
            corrected_value AS CorrectedValue,
            reason_for_correction AS ReasonForCorrection,
            supporting_evidence AS SupportingEvidence,
            downstream_impact_reviewed AS DownstreamImpactReviewed,
            effective_date_implications AS EffectiveDateImplications,
            requested_by AS RequestedBy,
            requested_date AS RequestedDate,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate,
            status AS Status
        FROM teaching.teaching_record_corrections
        WHERE id = @Id;
        """;

    private const string _updateCorrectionSql = """
        UPDATE teaching.teaching_record_corrections
        SET
            status = 'approved',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            updated_at = SYSUTCDATETIME(),
            updated_by = @ApprovedBy
        WHERE id = @Id AND status = 'requested';
        """;

    public async Task<Result<TeachingRecordCorrectionDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<TeachingRecordCorrectionDto>(
                DomainError.Validation("Teaching.EmptyCorrectionId", "Record correction ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify correction exists
        var existing = await connection.QuerySingleOrDefaultAsync<TeachingRecordCorrectionDto>(new CommandDefinition(
            _selectCorrectionSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<TeachingRecordCorrectionDto>(DomainError.NotFound(
                "Teaching.CorrectionNotFound",
                $"Record correction with ID '{id}' was not found."));
        }

        // 2. Validate status is 'requested'
        if (!string.Equals(existing.Status, "requested", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<TeachingRecordCorrectionDto>(DomainError.Conflict(
                "Teaching.InvalidCorrectionStatus",
                $"Record correction '{existing.CorrectionNumber}' cannot be approved because its current status is '{existing.Status}'. Only corrections in 'requested' status can be approved."));
        }

        // 3. Resolve canonical table and field metadata
        if (!RecordCorrectionColumnHelper.TryNormalizeTargetTable(existing.TargetTableName, out var canonicalTable))
        {
            return Result.Failure<TeachingRecordCorrectionDto>(DomainError.Conflict(
                "Teaching.InvalidTargetTable",
                $"Target table '{existing.TargetTableName}' associated with correction '{existing.CorrectionNumber}' is not recognized."));
        }

        if (!RecordCorrectionColumnHelper.TryGetColumnMetadata(canonicalTable, existing.FieldName, out var columnMetadata))
        {
            return Result.Failure<TeachingRecordCorrectionDto>(DomainError.Conflict(
                "Teaching.InvalidTargetField",
                $"Field '{existing.FieldName}' associated with correction '{existing.CorrectionNumber}' is not eligible for mutation."));
        }

        // 4. Verify target record exists in target table
        var targetExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            $"SELECT TOP 1 1 FROM teaching.{canonicalTable} WHERE id = @TargetRecordId;",
            new { TargetRecordId = existing.TargetRecordId },
            cancellationToken: cancellationToken));

        if (!targetExists)
        {
            return Result.Failure<TeachingRecordCorrectionDto>(DomainError.NotFound(
                "Teaching.TargetRecordNotFound",
                $"Target record with ID '{existing.TargetRecordId}' in table '{canonicalTable}' was not found."));
        }

        var approvedBy = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayDt = today.ToDateTime(TimeOnly.MinValue);

        // Prepare parameter value for SQL column update
        object? sqlTargetValue = existing.CorrectedValue;
        if (columnMetadata.IsDate)
        {
            if (string.IsNullOrWhiteSpace(existing.CorrectedValue) || string.Equals(existing.CorrectedValue.Trim(), "null", StringComparison.OrdinalIgnoreCase))
            {
                sqlTargetValue = DBNull.Value;
            }
            else if (DateOnly.TryParse(existing.CorrectedValue.Trim(), out var parsedDate))
            {
                sqlTargetValue = parsedDate.ToDateTime(TimeOnly.MinValue);
            }
        }
        else if (columnMetadata.IsNullable && (string.Equals(existing.CorrectedValue.Trim(), "null", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(existing.CorrectedValue)))
        {
            sqlTargetValue = DBNull.Value;
        }

        // 5. Atomic Execution within SqlTransaction
        if (connection is SqlConnection sqlConn && sqlConn.State != System.Data.ConnectionState.Open)
        {
            await sqlConn.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
                _updateCorrectionSql,
                new
                {
                    Id = id,
                    ApprovedBy = approvedBy,
                    ApprovedDate = todayDt
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<TeachingRecordCorrectionDto>(DomainError.Conflict(
                    "Teaching.InvalidCorrectionStatus",
                    $"Record correction '{existing.CorrectionNumber}' could not be approved because its status has changed."));
            }

            var updateTargetSql = $"""
                UPDATE teaching.{canonicalTable}
                SET
                    [{columnMetadata.CanonicalColumnName}] = @Val,
                    updated_at = SYSUTCDATETIME(),
                    updated_by = @ApprovedBy
                WHERE id = @TargetRecordId;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                updateTargetSql,
                new
                {
                    Val = sqlTargetValue,
                    ApprovedBy = approvedBy,
                    TargetRecordId = existing.TargetRecordId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // 6. Publish domain event
        var domainEvent = new TeachingRecordCorrectedEvent(
            id,
            existing.CorrectionNumber,
            canonicalTable,
            existing.TargetRecordId,
            columnMetadata.CanonicalColumnName,
            approvedBy);

        await publisher.Publish(domainEvent, cancellationToken);

        var updatedDto = existing with
        {
            Status = "approved",
            ApprovedBy = approvedBy,
            ApprovedDate = today
        };

        return Result.Success(updatedDto);
    }
}
