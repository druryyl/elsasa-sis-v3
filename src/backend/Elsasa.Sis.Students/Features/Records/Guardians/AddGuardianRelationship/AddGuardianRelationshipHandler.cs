namespace Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Handler for adding a guardian relationship to an enrolled student (ADR-004, ADR-005).
/// Deduplicates guardians against students.guardians (by NIK or name + phone),
/// atomically demotes prior active primary contact if IsPrimaryContact is true,
/// and persists the new relationship row.
/// </summary>
public sealed class AddGuardianRelationshipHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectStudentExistsSql = """
        SELECT id
        FROM students.students
        WHERE id = @StudentId;
        """;

    private const string _selectGuardianByIdSql = """
        SELECT id
        FROM students.guardians
        WHERE id = @GuardianId;
        """;

    private const string _selectGuardianByGovIdSql = """
        SELECT TOP 1 id
        FROM students.guardians
        WHERE government_id_number = @GovernmentIdNumber;
        """;

    private const string _selectGuardianByNameAndPhoneSql = """
        SELECT TOP 1 id
        FROM students.guardians
        WHERE full_name = @FullName AND phone = @Phone;
        """;

    private const string _selectActiveRelationshipSql = """
        SELECT id
        FROM students.student_guardian_relationships
        WHERE student_id = @StudentId AND guardian_id = @GuardianId AND is_active = 1;
        """;

    private const string _insertGuardianSql = """
        INSERT INTO students.guardians
        (
            id,
            full_name,
            government_id_type,
            government_id_number,
            phone,
            email,
            address,
            occupation,
            notes,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @FullName,
            @GovernmentIdType,
            @GovernmentIdNumber,
            @Phone,
            @Email,
            @Address,
            @Occupation,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _demotePrimaryContactsSql = """
        UPDATE students.student_guardian_relationships
        SET is_primary_contact = 0,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE student_id = @StudentId AND is_primary_contact = 1 AND is_active = 1;
        """;

    private const string _insertRelationshipSql = """
        INSERT INTO students.student_guardian_relationships
        (
            id,
            student_id,
            guardian_id,
            relationship_type,
            is_primary_contact,
            is_emergency_contact,
            is_financial_contact,
            is_authorized_pickup,
            is_active,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @StudentId,
            @GuardianId,
            @RelationshipType,
            @IsPrimaryContact,
            @IsEmergencyContact,
            @IsFinancialContact,
            @IsAuthorizedPickup,
            1,
            @CreatedAt,
            @CreatedBy
        );
        """;

    public async Task<Result<AddGuardianRelationshipResponse>> HandleAsync(
        Guid studentId,
        AddGuardianRelationshipRequest request,
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
            return Result.Failure<AddGuardianRelationshipResponse>(
                DomainError.NotFound(
                    "Students.StudentNotFound",
                    $"Student with ID '{studentId}' was not found."));
        }

        var fullName = request.FullName.Trim();
        var govIdType = string.IsNullOrWhiteSpace(request.GovernmentIdType) ? null : request.GovernmentIdType.Trim();
        var govIdNumber = string.IsNullOrWhiteSpace(request.GovernmentIdNumber) ? null : request.GovernmentIdNumber.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        var occupation = string.IsNullOrWhiteSpace(request.Occupation) ? null : request.Occupation.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var relationshipType = GuardianRelationshipTypes.Normalize(request.RelationshipType);
        var auditUser = currentUser.Username ?? "system";
        var now = DateTimeOffset.UtcNow;

        // 2. Guardian deduplication & resolution
        Guid? existingGuardianId = null;

        if (request.GuardianId.HasValue)
        {
            existingGuardianId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                _selectGuardianByIdSql,
                new { GuardianId = request.GuardianId.Value },
                cancellationToken: cancellationToken));

            if (existingGuardianId is null)
            {
                return Result.Failure<AddGuardianRelationshipResponse>(
                    DomainError.NotFound(
                        "Students.Guardians.GuardianNotFound",
                        $"Guardian with ID '{request.GuardianId.Value}' was not found."));
            }
        }
        else if (!string.IsNullOrWhiteSpace(govIdNumber))
        {
            existingGuardianId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                _selectGuardianByGovIdSql,
                new { GovernmentIdNumber = govIdNumber },
                cancellationToken: cancellationToken));
        }
        else if (!string.IsNullOrWhiteSpace(phone))
        {
            existingGuardianId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                _selectGuardianByNameAndPhoneSql,
                new { FullName = fullName, Phone = phone },
                cancellationToken: cancellationToken));
        }

        // 3. Prevent duplicate active relationships for the same student & guardian
        if (existingGuardianId.HasValue)
        {
            var activeRelExists = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                _selectActiveRelationshipSql,
                new { StudentId = studentId, GuardianId = existingGuardianId.Value },
                cancellationToken: cancellationToken));

            if (activeRelExists.HasValue)
            {
                return Result.Failure<AddGuardianRelationshipResponse>(
                    DomainError.Conflict(
                        "Students.Guardians.RelationshipAlreadyExists",
                        $"An active relationship already exists between student '{studentId}' and guardian '{existingGuardianId.Value}'."));
            }
        }

        var isNewGuardian = !existingGuardianId.HasValue;
        var guardianId = existingGuardianId ?? Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        // 4. Atomic transaction: insert guardian (if new), demote prior primary contact, and insert relationship
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            if (isNewGuardian)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertGuardianSql,
                    new
                    {
                        Id = guardianId,
                        FullName = fullName,
                        GovernmentIdType = govIdType,
                        GovernmentIdNumber = govIdNumber,
                        Phone = phone,
                        Email = email,
                        Address = address,
                        Occupation = occupation,
                        Notes = notes,
                        CreatedAt = now,
                        CreatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
            }

            if (request.IsPrimaryContact)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _demotePrimaryContactsSql,
                    new
                    {
                        StudentId = studentId,
                        UpdatedAt = now,
                        UpdatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertRelationshipSql,
                new
                {
                    Id = relationshipId,
                    StudentId = studentId,
                    GuardianId = guardianId,
                    RelationshipType = relationshipType,
                    IsPrimaryContact = request.IsPrimaryContact,
                    IsEmergencyContact = request.IsEmergencyContact,
                    IsFinancialContact = request.IsFinancialContact,
                    IsAuthorizedPickup = request.IsAuthorizedPickup,
                    CreatedAt = now,
                    CreatedBy = auditUser
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return Result.Success(new AddGuardianRelationshipResponse(relationshipId, guardianId));
    }
}
