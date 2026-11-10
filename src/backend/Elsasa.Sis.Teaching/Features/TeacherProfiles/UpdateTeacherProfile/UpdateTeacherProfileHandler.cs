namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.UpdateTeacherProfile;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing mutable teacher profile updates while strictly preventing modification
/// of standing, approved_by, or identity keys (P2-S05).
/// </summary>
public sealed class UpdateTeacherProfileHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectTeacherSql = """
        SELECT
            id AS Id,
            standing AS Standing,
            approved_by AS ApprovedBy
        FROM teaching.teacher_profiles
        WHERE id = @Id;
        """;

    private const string _updateTeacherSql = """
        UPDATE teaching.teacher_profiles
        SET
            display_name = @DisplayName,
            email = @Email,
            phone = @Phone,
            campus_affiliation = COALESCE(@CampusAffiliation, campus_affiliation),
            department = @Department,
            notes = @Notes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    public async Task<Result> HandleAsync(
        Guid id,
        UpdateTeacherProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<TeacherProfileRow>(new CommandDefinition(
            _selectTeacherSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{id}' was not found."));
        }

        var normalizedDisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var normalizedCampus = string.IsNullOrWhiteSpace(request.CampusAffiliation) ? null : request.CampusAffiliation.Trim();
        var normalizedDepartment = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim();
        var normalizedNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var auditUser = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _updateTeacherSql,
            new
            {
                Id = id,
                DisplayName = normalizedDisplayName,
                Email = normalizedEmail,
                Phone = normalizedPhone,
                CampusAffiliation = normalizedCampus,
                Department = normalizedDepartment,
                Notes = normalizedNotes,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        return Result.Success();
    }

    private sealed record TeacherProfileRow(Guid Id, string Standing, string? ApprovedBy);
}
