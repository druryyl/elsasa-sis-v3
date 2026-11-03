namespace Elsasa.Sis.Students.Features.Records.Guardians.UpdateGuardianRelationship;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing update of an existing student-guardian relationship (ADR-004, ADR-005).
/// Atomically handles primary contact reassignment (demoting any other active primary contact
/// if IsPrimaryContact = true and IsActive = true), updating responsibility flags and active status.
/// </summary>
public sealed class UpdateGuardianRelationshipHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectStudentExistsSql = """
        SELECT id
        FROM students.students
        WHERE id = @StudentId;
        """;

    private const string _selectRelationshipSql = """
        SELECT
            id AS Id,
            guardian_id AS GuardianId,
            relationship_type AS RelationshipType,
            is_primary_contact AS IsPrimaryContact,
            is_active AS IsActive
        FROM students.student_guardian_relationships
        WHERE id = @RelationshipId AND student_id = @StudentId;
        """;

    private const string _demoteOtherPrimaryContactsSql = """
        UPDATE students.student_guardian_relationships
        SET is_primary_contact = 0,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE student_id = @StudentId
          AND id <> @RelationshipId
          AND is_primary_contact = 1
          AND is_active = 1;
        """;

    private const string _updateRelationshipSql = """
        UPDATE students.student_guardian_relationships
        SET relationship_type = @RelationshipType,
            is_primary_contact = @IsPrimaryContact,
            is_emergency_contact = @IsEmergencyContact,
            is_financial_contact = @IsFinancialContact,
            is_authorized_pickup = @IsAuthorizedPickup,
            is_active = @IsActive,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @RelationshipId AND student_id = @StudentId;
        """;

    private const string _updateGuardianMasterSql = """
        UPDATE students.guardians
        SET phone = COALESCE(@Phone, phone),
            email = COALESCE(@Email, email),
            address = COALESCE(@Address, address),
            occupation = COALESCE(@Occupation, occupation),
            notes = COALESCE(@Notes, notes),
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @GuardianId;
        """;

    public async Task<Result<UpdateGuardianRelationshipResponse>> HandleAsync(
        Guid studentId,
        Guid relationshipId,
        UpdateGuardianRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify student exists
        var studentExists = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            _selectStudentExistsSql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        if (studentExists is null)
        {
            return Result.Failure<UpdateGuardianRelationshipResponse>(
                DomainError.NotFound(
                    "Students.StudentNotFound",
                    $"Student with ID '{studentId}' was not found."));
        }

        // 2. Verify relationship exists
        var existingRel = await connection.QuerySingleOrDefaultAsync<ExistingRelationshipRow>(new CommandDefinition(
            _selectRelationshipSql,
            new { RelationshipId = relationshipId, StudentId = studentId },
            cancellationToken: cancellationToken));

        if (existingRel is null)
        {
            return Result.Failure<UpdateGuardianRelationshipResponse>(
                DomainError.NotFound(
                    "Students.Guardians.RelationshipNotFound",
                    $"Guardian relationship with ID '{relationshipId}' was not found for student '{studentId}'."));
        }

        var relationshipType = GuardianRelationshipTypes.Normalize(request.RelationshipType);
        var auditUser = currentUser.Username ?? "system";
        var now = DateTimeOffset.UtcNow;
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        var occupation = string.IsNullOrWhiteSpace(request.Occupation) ? null : request.Occupation.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        var shouldUpdateGuardianMaster = phone is not null
            || email is not null
            || address is not null
            || occupation is not null
            || notes is not null;

        // 3. Atomic transaction: demote other primary contacts if needed, then update relationship and guardian master
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            if (request.IsPrimaryContact && request.IsActive)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _demoteOtherPrimaryContactsSql,
                    new
                    {
                        StudentId = studentId,
                        RelationshipId = relationshipId,
                        UpdatedAt = now,
                        UpdatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _updateRelationshipSql,
                new
                {
                    RelationshipId = relationshipId,
                    StudentId = studentId,
                    RelationshipType = relationshipType,
                    IsPrimaryContact = request.IsPrimaryContact,
                    IsEmergencyContact = request.IsEmergencyContact,
                    IsFinancialContact = request.IsFinancialContact,
                    IsAuthorizedPickup = request.IsAuthorizedPickup,
                    IsActive = request.IsActive,
                    UpdatedAt = now,
                    UpdatedBy = auditUser
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            if (shouldUpdateGuardianMaster)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _updateGuardianMasterSql,
                    new
                    {
                        GuardianId = existingRel.GuardianId,
                        Phone = phone,
                        Email = email,
                        Address = address,
                        Occupation = occupation,
                        Notes = notes,
                        UpdatedAt = now,
                        UpdatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
            }

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return Result.Success(new UpdateGuardianRelationshipResponse(
            RelationshipId: relationshipId,
            GuardianId: existingRel.GuardianId,
            RelationshipType: relationshipType,
            IsPrimaryContact: request.IsPrimaryContact,
            IsEmergencyContact: request.IsEmergencyContact,
            IsFinancialContact: request.IsFinancialContact,
            IsAuthorizedPickup: request.IsAuthorizedPickup,
            IsActive: request.IsActive));
    }

    private sealed record ExistingRelationshipRow(
        Guid Id,
        Guid GuardianId,
        string RelationshipType,
        bool IsPrimaryContact,
        bool IsActive);
}
