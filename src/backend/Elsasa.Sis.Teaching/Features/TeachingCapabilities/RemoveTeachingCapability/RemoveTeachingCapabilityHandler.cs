namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.RemoveTeachingCapability;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler soft-retiring a potential teaching capability (sets status = 'inactive') (P2-S08).
/// </summary>
public sealed class RemoveTeachingCapabilityHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectCapabilitySql = """
        SELECT id AS Id, status AS Status
        FROM teaching.teaching_capabilities
        WHERE id = @CapabilityId AND teacher_profile_id = @TeacherId;
        """;

    private const string _retireCapabilitySql = """
        UPDATE teaching.teaching_capabilities
        SET status = 'inactive',
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @CapabilityId;
        """;

    public async Task<Result> HandleAsync(
        Guid teacherId,
        Guid capabilityId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<CapabilityRow>(new CommandDefinition(
            _selectCapabilitySql,
            new { CapabilityId = capabilityId, TeacherId = teacherId },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure(DomainError.NotFound(
                "Teaching.TeachingCapabilityNotFound",
                $"Teaching capability with ID '{capabilityId}' for teacher '{teacherId}' was not found."));
        }

        if (string.Equals(existing.Status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            // Already retired - idempotent success
            return Result.Success();
        }

        var auditUser = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _retireCapabilitySql,
            new { CapabilityId = capabilityId, UpdatedBy = auditUser },
            cancellationToken: cancellationToken));

        return Result.Success();
    }

    private sealed record CapabilityRow(Guid Id, string Status);
}
