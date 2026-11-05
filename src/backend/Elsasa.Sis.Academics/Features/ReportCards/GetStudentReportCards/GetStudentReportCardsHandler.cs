namespace Elsasa.Sis.Academics.Features.ReportCards.GetStudentReportCards;

using Dapper;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for querying published student report cards in the self-service portal (UC-ACD-13, GAP-006, SCR-SELF-03, P5-S12).
/// Strictly queries certified report cards in 'published' status scoped to the authenticated student or authorized parent/staff.
/// </summary>
public sealed class GetStudentReportCardsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static GetStudentReportCardsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private static readonly HashSet<string> _privilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Teacher",
        "AcademicCoordinator",
        "Academic Coordinator",
        "AcademicAuthority",
        "Academic Authority",
        "Admin",
        "Administrator"
    };

    private const string _selectStudentPublishedReportCardsSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            academic_period_id AS AcademicPeriodId,
            academic_period_label AS PeriodLabel,
            report_type AS ReportType,
            template_name AS TemplateName,
            status AS Status,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate,
            published_date AS PublishedDate,
            narrative_comments AS NarrativeComments,
            created_at AS CreatedAt
        FROM academics.report_cards
        WHERE student_id = @StudentId
          AND status = 'published'
          AND (@AcademicPeriodId IS NULL OR academic_period_id = @AcademicPeriodId)
        ORDER BY published_date DESC, created_at DESC;
        """;

    private sealed record ReportCardSummaryRow(
        Guid Id,
        Guid StudentId,
        Guid AcademicPeriodId,
        string PeriodLabel,
        string ReportType,
        string TemplateName,
        string Status,
        string? ApprovedBy,
        DateOnly? ApprovedDate,
        DateOnly? PublishedDate,
        string? NarrativeComments,
        DateTimeOffset CreatedAt);

    public async Task<Result<IReadOnlyList<ReportCardSummaryDto>>> HandleAsync(
        Guid? studentIdOverride = null,
        Guid? academicPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        var isPrivileged = currentUser.Roles.Any(r => _privilegedRoles.Contains(r));
        var isParent = currentUser.Roles.Any(r => r.Equals("Parent", StringComparison.OrdinalIgnoreCase));

        Guid effectiveStudentId;
        if (currentUser.StudentId.HasValue)
        {
            if (studentIdOverride.HasValue && studentIdOverride.Value != currentUser.StudentId.Value && !isPrivileged)
            {
                return Result.Failure<IReadOnlyList<ReportCardSummaryDto>>(
                    DomainError.Forbidden(
                        "Academics.ReportCards.UnauthorizedPortalAccess",
                        "Students are only authorized to access their own report cards."));
            }

            effectiveStudentId = currentUser.StudentId.Value;
        }
        else if (isPrivileged || isParent)
        {
            if (!studentIdOverride.HasValue || studentIdOverride.Value == Guid.Empty)
            {
                return Result.Failure<IReadOnlyList<ReportCardSummaryDto>>(
                    DomainError.Validation(
                        "Academics.ReportCards.StudentIdRequired",
                        "Student ID must be specified when querying student report cards as a staff member or parent."));
            }

            effectiveStudentId = studentIdOverride.Value;
        }
        else
        {
            return Result.Failure<IReadOnlyList<ReportCardSummaryDto>>(
                DomainError.Forbidden(
                    "Academics.ReportCards.UnauthorizedPortalAccess",
                    "The current user is not associated with an enrolled student."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ReportCardSummaryRow>(new CommandDefinition(
            _selectStudentPublishedReportCardsSql,
            new
            {
                StudentId = effectiveStudentId,
                AcademicPeriodId = academicPeriodId
            },
            cancellationToken: cancellationToken));

        var results = rows.Select(r => new ReportCardSummaryDto(
            Id: r.Id,
            StudentId: r.StudentId,
            AcademicPeriodId: r.AcademicPeriodId,
            PeriodLabel: r.PeriodLabel,
            ReportType: r.ReportType,
            TemplateName: r.TemplateName,
            Status: r.Status,
            ApprovedBy: r.ApprovedBy,
            ApprovedDate: r.ApprovedDate,
            PublishedDate: r.PublishedDate,
            NarrativeComments: r.NarrativeComments,
            CreatedAt: r.CreatedAt
        )).ToList();

        return Result.Success<IReadOnlyList<ReportCardSummaryDto>>(results);
    }
}
