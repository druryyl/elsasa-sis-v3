namespace Elsasa.Sis.Api.Features.Dashboard.GetExecutiveDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using AcademicsPermissions = Elsasa.Sis.Academics.Permissions;
using FinancePermissions = Elsasa.Sis.Finance.Permissions;
using OperationsPermissions = Elsasa.Sis.Operations.Permissions;
using StudentsPermissions = Elsasa.Sis.Students.Permissions;
using TeachingPermissions = Elsasa.Sis.Teaching.Permissions;

/// <summary>
/// Orchestrates the Level 1 Executive Overview dashboard by resolving period context,
/// invoking all domain <see cref="IDomainKpiSummaryProvider"/> implementations sequentially,
/// appending the Stage 3 Development readiness placeholder card, evaluating role/permission access,
/// and prioritizing the cross-domain attention queue.
/// </summary>
public sealed class GetExecutiveDashboardHandler(
    IKpiPeriodResolver periodResolver,
    IEnumerable<IDomainKpiSummaryProvider> summaryProviders,
    ICurrentUser currentUser)
{
    private const int _maxNeedsAttentionSignals = 12;

    private static readonly DomainAccessRule _studentsAccessRule = new(
        AllowedPermissions:
        [
            "*",
            StudentsPermissions.Students.Admission.View,
            StudentsPermissions.Students.Records.View
        ],
        AllowedRoles:
        [
            "Admin",
            "SuperAdmin",
            "AdmissionOfficer",
            "Admission Officer",
            "StudentAdminOfficer",
            "Student Administration Officer",
            "SchoolLeadership",
            "School Leadership"
        ]);

    private static readonly DomainAccessRule _academicsAccessRule = new(
        AllowedPermissions:
        [
            "*",
            AcademicsPermissions.Academics.Participations.View,
            "Permissions.Academics.Coursework.View"
        ],
        AllowedRoles:
        [
            "Admin",
            "SuperAdmin",
            "AcademicAdmin",
            "AcademicCoordinator",
            "Academic Coordinator",
            "AcademicAuthority",
            "Academic Authority",
            "Teacher"
        ]);

    private static readonly DomainAccessRule _financeAccessRule = new(
        AllowedPermissions:
        [
            "*",
            FinancePermissions.Obligations.View
        ],
        AllowedRoles:
        [
            "Admin",
            "SuperAdmin",
            "FinanceAuthority",
            "Finance Authority",
            "FinanceOfficer",
            "Finance Officer",
            "FinanceStaff",
            "Finance Staff"
        ]);

    private static readonly DomainAccessRule _teachingAccessRule = new(
        AllowedPermissions:
        [
            "*",
            TeachingPermissions.Faculty.View,
            TeachingPermissions.Workload.View
        ],
        AllowedRoles:
        [
            "Admin",
            "SuperAdmin",
            "AcademicCoordinator",
            "Academic Coordinator",
            "AcademicAuthority",
            "Academic Authority",
            "TeachingAuthority",
            "Teaching Authority",
            "TeachingCoordinator",
            "Teaching Coordinator",
            "Teacher"
        ]);

    private static readonly DomainAccessRule _operationsAccessRule = new(
        AllowedPermissions:
        [
            "*",
            OperationsPermissions.Operations.Calendar.View,
            OperationsPermissions.Operations.Calendar.Manage
        ],
        AllowedRoles:
        [
            "Admin",
            "SuperAdmin",
            "OperationsAuthority",
            "Operations Authority",
            "AcademicCoordinator",
            "Academic Coordinator",
            "AcademicAdmin"
        ]);

    private static readonly DomainHealthSummaryDto _developmentPlaceholderCard = new(
        DomainKey: "development",
        DomainName: "Student Development",
        Status: "pending_stage",
        StatusLabel: "Stage 3 Pending",
        HeadlineLabel: "Behaviour & Wellbeing",
        HeadlineValue: "Stage 3",
        SecondaryLabel: "Target Scope",
        SecondaryValue: "DEV-01 – DEV-05",
        WarningCount: 0,
        CriticalCount: 0,
        DashboardRoute: "/dashboard/development",
        IsAccessible: true);

    /// <summary>
    /// Computes the Level 1 Executive Overview response for the supplied global filter.
    /// </summary>
    /// <param name="filter">Incoming global KPI filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated executive dashboard response.</returns>
    public async Task<Result<ExecutiveDashboardResponse>> HandleAsync(
        GlobalKpiFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var context = await periodResolver.ResolveContextAsync(filter, cancellationToken);
        var availablePeriods = await periodResolver.GetAvailablePeriodsAsync(context.AcademicYearId, cancellationToken);

        // Execute domain providers sequentially so each independent SqlConnection completes cleanly without MARS contention (RISK-002).
        var providerResults = new List<DomainKpiSummaryResult>();
        foreach (var provider in summaryProviders)
        {
            var summary = await provider.GetSummaryAsync(context, cancellationToken);
            providerResults.Add(summary);
        }

        var orderedResults = providerResults
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.DomainHealth.DomainKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var headlineKpis = orderedResults
            .Select(r => r.HeadlineKpi)
            .ToList();

        var domainSummaries = new List<DomainHealthSummaryDto>(orderedResults.Count + 1);
        var accessibleSignals = new List<AttentionSignalDto>();

        foreach (var result in orderedResults)
        {
            var isAccessible = HasDomainAccess(result.DomainHealth.DomainKey, currentUser);
            domainSummaries.Add(result.DomainHealth with { IsAccessible = isAccessible });

            if (isAccessible)
            {
                accessibleSignals.AddRange(result.AttentionSignals);
            }
        }

        domainSummaries.Add(_developmentPlaceholderCard);

        var prioritizedQueue = accessibleSignals
            .OrderBy(s => string.Equals(s.Severity, "critical", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenByDescending(s => s.AffectedCount)
            .ThenBy(s => s.SignalCode, StringComparer.OrdinalIgnoreCase)
            .Take(_maxNeedsAttentionSignals)
            .ToList();

        return Result.Success(new ExecutiveDashboardResponse(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Context: context,
            AvailablePeriods: availablePeriods,
            HeadlineKpis: headlineKpis,
            DomainSummaries: domainSummaries,
            NeedsAttentionQueue: prioritizedQueue));
    }

    private static bool HasDomainAccess(string domainKey, ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated)
        {
            return false;
        }

        var rule = domainKey.ToLowerInvariant() switch
        {
            "students" => _studentsAccessRule,
            "academics" => _academicsAccessRule,
            "finance" => _financeAccessRule,
            "teaching" => _teachingAccessRule,
            "operations" => _operationsAccessRule,
            _ => null
        };

        if (rule is null)
        {
            return true;
        }

        if (currentUser.Permissions.Any(p =>
                rule.AllowedPermissions.Contains(p, StringComparer.OrdinalIgnoreCase)))
        {
            return true;
        }

        return currentUser.Roles.Any(r =>
            rule.AllowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    private sealed record DomainAccessRule(
        string[] AllowedPermissions,
        string[] AllowedRoles);
}
