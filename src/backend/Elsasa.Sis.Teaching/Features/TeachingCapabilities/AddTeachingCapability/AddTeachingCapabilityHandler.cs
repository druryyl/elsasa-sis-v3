namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.AddTeachingCapability;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler recording a potential teaching capability on a teacher profile (P2-S08).
/// Confirms capability describes potential suitability only; does not alter profile standing or authorizations.
/// Enforces UQ_teaching_capabilities_unique idempotently.
/// </summary>
public sealed class AddTeachingCapabilityHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _checkTeacherSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE id = @TeacherId;
        """;

    private const string _checkCapabilityTypeSql = """
        SELECT TOP 1 1
        FROM teaching.capability_types
        WHERE id = @CapabilityTypeId AND is_active = 1;
        """;

    private const string _selectExistingCapabilitySql = """
        SELECT id AS Id, status AS Status
        FROM teaching.teaching_capabilities
        WHERE teacher_profile_id = @TeacherProfileId
          AND capability_code = @CapabilityCode
          AND ((applicable_level IS NULL AND @ApplicableLevel IS NULL) OR applicable_level = @ApplicableLevel)
          AND ((delivery_mode IS NULL AND @DeliveryMode IS NULL) OR delivery_mode = @DeliveryMode);
        """;

    private const string _reactivateCapabilitySql = """
        UPDATE teaching.teaching_capabilities
        SET capability_type_id = @CapabilityTypeId,
            capability_name = @CapabilityName,
            language_of_instruction = @LanguageOfInstruction,
            status = 'active',
            notes = @Notes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private const string _insertCapabilitySql = """
        INSERT INTO teaching.teaching_capabilities (
            id,
            teacher_profile_id,
            capability_type_id,
            capability_code,
            capability_name,
            applicable_level,
            delivery_mode,
            language_of_instruction,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherProfileId,
            @CapabilityTypeId,
            @CapabilityCode,
            @CapabilityName,
            @ApplicableLevel,
            @DeliveryMode,
            @LanguageOfInstruction,
            'active',
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<AddTeachingCapabilityResponse>> HandleAsync(
        Guid teacherId,
        AddTeachingCapabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify teacher profile exists
        var teacherExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkTeacherSql,
            new { TeacherId = teacherId },
            cancellationToken: cancellationToken));

        if (!teacherExists)
        {
            return Result.Failure<AddTeachingCapabilityResponse>(DomainError.NotFound(
                "Teaching.TeacherProfileNotFound",
                $"Teacher profile with ID '{teacherId}' was not found."));
        }

        // 2. Verify capability type exists and is active
        var capabilityTypeExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkCapabilityTypeSql,
            new { CapabilityTypeId = request.CapabilityTypeId },
            cancellationToken: cancellationToken));

        if (!capabilityTypeExists)
        {
            return Result.Failure<AddTeachingCapabilityResponse>(DomainError.NotFound(
                "Teaching.CapabilityTypeNotFound",
                $"The capability type with ID '{request.CapabilityTypeId}' does not exist or is inactive."));
        }

        var normalizedCode = request.CapabilityCode.Trim();
        var normalizedName = request.CapabilityName.Trim();
        var normalizedLevel = string.IsNullOrWhiteSpace(request.ApplicableLevel) ? null : request.ApplicableLevel.Trim();
        var normalizedMode = string.IsNullOrWhiteSpace(request.DeliveryMode) ? null : request.DeliveryMode.Trim();
        var normalizedLang = string.IsNullOrWhiteSpace(request.LanguageOfInstruction) ? null : request.LanguageOfInstruction.Trim();
        var normalizedNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var auditUser = currentUser.Username ?? "system";

        // 3. Enforce UQ_teaching_capabilities_unique idempotently
        var existing = await connection.QuerySingleOrDefaultAsync<ExistingCapabilityRow>(new CommandDefinition(
            _selectExistingCapabilitySql,
            new
            {
                TeacherProfileId = teacherId,
                CapabilityCode = normalizedCode,
                ApplicableLevel = normalizedLevel,
                DeliveryMode = normalizedMode
            },
            cancellationToken: cancellationToken));

        if (existing is not null)
        {
            if (string.Equals(existing.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                // Already active - idempotent return
                return Result.Success(new AddTeachingCapabilityResponse(existing.Id));
            }

            // Reactivate previously retired capability
            await connection.ExecuteAsync(new CommandDefinition(
                _reactivateCapabilitySql,
                new
                {
                    Id = existing.Id,
                    CapabilityTypeId = request.CapabilityTypeId,
                    CapabilityName = normalizedName,
                    LanguageOfInstruction = normalizedLang,
                    Notes = normalizedNotes,
                    UpdatedBy = auditUser
                },
                cancellationToken: cancellationToken));

            return Result.Success(new AddTeachingCapabilityResponse(existing.Id));
        }

        var newCapabilityId = Guid.NewGuid();

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertCapabilitySql,
                new
                {
                    Id = newCapabilityId,
                    TeacherProfileId = teacherId,
                    CapabilityTypeId = request.CapabilityTypeId,
                    CapabilityCode = normalizedCode,
                    CapabilityName = normalizedName,
                    ApplicableLevel = normalizedLevel,
                    DeliveryMode = normalizedMode,
                    LanguageOfInstruction = normalizedLang,
                    Notes = normalizedNotes,
                    CreatedBy = auditUser
                },
                cancellationToken: cancellationToken));

            return Result.Success(new AddTeachingCapabilityResponse(newCapabilityId));
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            // Concurrent insert race condition: fetch existing ID and return idempotently
            var concurrent = await connection.QuerySingleOrDefaultAsync<ExistingCapabilityRow>(new CommandDefinition(
                _selectExistingCapabilitySql,
                new
                {
                    TeacherProfileId = teacherId,
                    CapabilityCode = normalizedCode,
                    ApplicableLevel = normalizedLevel,
                    DeliveryMode = normalizedMode
                },
                cancellationToken: cancellationToken));

            if (concurrent is not null)
            {
                return Result.Success(new AddTeachingCapabilityResponse(concurrent.Id));
            }

            throw;
        }
    }

    private sealed record ExistingCapabilityRow(Guid Id, string Status);
}
