namespace Elsasa.Sis.Finance.Features.Billing.Common;

using Dapper;
using System.Data;

/// <summary>
/// Resolved billing document recipient contact details.
/// </summary>
public sealed record BillingRecipient(
    string Name,
    string Relationship,
    string Contact);

/// <summary>
/// Helper resolving primary recipient according to the guarantor hierarchy (GAP-005, TD-05):
/// 1. Primary financial contact (is_financial_contact = 1) from active guardian relationships.
/// 2. Primary contact (is_primary_contact = 1) from active guardian relationships.
/// 3. Earliest active guardian relationship.
/// 4. Fallback to student self identity.
/// </summary>
public static class BillingRecipientResolver
{
    private const string _resolveGuardianSql = """
        SELECT TOP (1)
            g.full_name AS Name,
            rel.relationship_type AS Relationship,
            COALESCE(NULLIF(g.phone, ''), NULLIF(g.email, ''), '-') AS Contact
        FROM students.student_guardian_relationships rel
        INNER JOIN students.guardians g ON g.id = rel.guardian_id
        WHERE rel.student_id = @StudentId AND rel.is_active = 1
        ORDER BY 
            rel.is_financial_contact DESC,
            rel.is_primary_contact DESC,
            rel.created_at ASC;
        """;

    private const string _resolveStudentSql = """
        SELECT 
            full_name AS Name,
            'Self' AS Relationship,
            COALESCE(NULLIF(phone, ''), NULLIF(email, ''), '-') AS Contact
        FROM students.students
        WHERE id = @StudentId;
        """;

    public static async Task<BillingRecipient?> ResolveAsync(
        Guid studentId,
        IDbConnection connection,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var guardian = await connection.QueryFirstOrDefaultAsync<BillingRecipient>(new CommandDefinition(
            _resolveGuardianSql,
            new { StudentId = studentId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (guardian is not null && !string.IsNullOrWhiteSpace(guardian.Name))
        {
            return guardian;
        }

        var student = await connection.QueryFirstOrDefaultAsync<BillingRecipient>(new CommandDefinition(
            _resolveStudentSql,
            new { StudentId = studentId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        return student;
    }
}
