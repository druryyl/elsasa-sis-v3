namespace Elsasa.Sis.Students.Features.Records.UpdateStudentIdentity;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing certified student identity updates (ADR-004, ADR-005).
/// </summary>
public sealed class UpdateStudentIdentityHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectStudentSql = """
        SELECT
            id AS Id,
            student_number AS StudentNumber
        FROM students.students
        WHERE id = @Id;
        """;

    private const string _checkDuplicateGovIdSql = """
        SELECT TOP 1 1
        FROM students.students
        WHERE government_id_number = @GovernmentIdNumber AND id <> @Id;
        """;

    private const string _updateStudentSql = """
        UPDATE students.students
        SET
            full_name = @FullName,
            preferred_name = @PreferredName,
            date_of_birth = @DateOfBirth,
            place_of_birth = @PlaceOfBirth,
            gender = @Gender,
            nationality = @Nationality,
            government_id_type = @GovernmentIdType,
            government_id_number = @GovernmentIdNumber,
            address = @Address,
            phone = @Phone,
            email = @Email,
            is_independent = @IsIndependent,
            notes = @Notes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Executes certified identity update for the specified student.
    /// </summary>
    /// <param name="id">The student identifier.</param>
    /// <param name="request">The updated identity attributes.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result indicating success or domain error.</returns>
    public async Task<Result> HandleAsync(
        Guid id,
        UpdateStudentIdentityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var student = await connection.QuerySingleOrDefaultAsync<StudentSummaryRow>(new CommandDefinition(
            _selectStudentSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (student is null)
        {
            return Result.Failure(DomainError.NotFound(
                "Students.StudentNotFound",
                $"Student with ID '{id}' was not found."));
        }

        var normalizedGovId = string.IsNullOrWhiteSpace(request.GovernmentIdNumber)
            ? null
            : request.GovernmentIdNumber.Trim();

        if (normalizedGovId is not null)
        {
            var hasDuplicateGovId = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkDuplicateGovIdSql,
                new { GovernmentIdNumber = normalizedGovId, Id = id },
                cancellationToken: cancellationToken));

            if (hasDuplicateGovId)
            {
                return Result.Failure(DomainError.Conflict(
                    "Students.DuplicateGovernmentId",
                    $"Government ID number '{normalizedGovId}' is already registered to another student."));
            }
        }

        var normalizedFullName = request.FullName.Trim();
        var normalizedPreferredName = string.IsNullOrWhiteSpace(request.PreferredName) ? null : request.PreferredName.Trim();
        var normalizedPlaceOfBirth = string.IsNullOrWhiteSpace(request.PlaceOfBirth) ? null : request.PlaceOfBirth.Trim();
        var normalizedGender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
        var normalizedNationality = string.IsNullOrWhiteSpace(request.Nationality) ? null : request.Nationality.Trim();
        var normalizedGovIdType = string.IsNullOrWhiteSpace(request.GovernmentIdType) ? null : request.GovernmentIdType.Trim();
        var normalizedAddress = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var normalizedNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var auditUser = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _updateStudentSql,
            new
            {
                Id = id,
                FullName = normalizedFullName,
                PreferredName = normalizedPreferredName,
                DateOfBirth = request.DateOfBirth,
                PlaceOfBirth = normalizedPlaceOfBirth,
                Gender = normalizedGender,
                Nationality = normalizedNationality,
                GovernmentIdType = normalizedGovIdType,
                GovernmentIdNumber = normalizedGovId,
                Address = normalizedAddress,
                Phone = normalizedPhone,
                Email = normalizedEmail,
                IsIndependent = request.IsIndependent,
                Notes = normalizedNotes,
                UpdatedBy = auditUser,
            },
            cancellationToken: cancellationToken));

        return Result.Success();
    }

    private sealed record StudentSummaryRow(Guid Id, string StudentNumber);
}
