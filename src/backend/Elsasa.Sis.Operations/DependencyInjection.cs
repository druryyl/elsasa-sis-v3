namespace Elsasa.Sis.Operations;

using Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ActivateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApplyAcademicYearPreset;
using Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApproveAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.GetAcademicYearById;
using Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.GetAcademicYearsList;
using Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.UpdateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.CreateCalendarEntry;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.DeleteCalendarEntry;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.GetCalendarMatrixProjection;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.ActivateOperationalPeriod;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CloseOperationalPeriod;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.GetOperationalPeriodsByAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.UpdateOperationalPeriod;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarAmendments.GetCalendarAmendmentsList;
using Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.CorrectOperationsRecord;
using Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.GetRecordCorrectionsList;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.GetActiveSchoolCalendar;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.GetSchoolCalendarsList;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.PublishSchoolCalendar;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.WithdrawSchoolCalendar;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.DeclareSchoolClosure;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.GetSchoolClosuresList;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.LiftSchoolClosure;
using Elsasa.Sis.Operations.Services;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Operations.Features.DemoData;
using Elsasa.Sis.Operations.Features.Kpis.GetOperationsKpiDashboard;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection registrations for the Operations domain assembly (ADR-001).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all feature slice handlers and domain services for the Operations bounded context.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddOperationsDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Academic Year lifecycle handlers
        services.AddScoped<CreateAcademicYearHandler>();
        services.AddScoped<UpdateAcademicYearHandler>();
        services.AddScoped<ApproveAcademicYearHandler>();
        services.AddScoped<ActivateAcademicYearHandler>();
        services.AddScoped<GetAcademicYearsListHandler>();
        services.AddScoped<GetAcademicYearByIdHandler>();
        services.AddScoped<ApplyAcademicYearPresetHandler>();

        // Operational Period handlers
        services.AddScoped<CreateOperationalPeriodHandler>();
        services.AddScoped<UpdateOperationalPeriodHandler>();
        services.AddScoped<ActivateOperationalPeriodHandler>();
        services.AddScoped<CloseOperationalPeriodHandler>();
        services.AddScoped<GetOperationalPeriodsByAcademicYearHandler>();

        // School Calendar handlers
        services.AddScoped<CreateSchoolCalendarHandler>();
        services.AddScoped<PublishSchoolCalendarHandler>();
        services.AddScoped<WithdrawSchoolCalendarHandler>();
        services.AddScoped<GetActiveSchoolCalendarHandler>();
        services.AddScoped<GetSchoolCalendarsListHandler>();
        // Calendar Entry & Matrix handlers
        services.AddScoped<CreateCalendarEntryHandler>();
        services.AddScoped<DeleteCalendarEntryHandler>();
        services.AddScoped<GetCalendarMatrixProjectionHandler>();

        // School Closure handlers
        services.AddScoped<DeclareSchoolClosureHandler>();
        services.AddScoped<LiftSchoolClosureHandler>();
        services.AddScoped<GetSchoolClosuresListHandler>();

        // Calendar Amendment handlers
        services.AddScoped<GetCalendarAmendmentsListHandler>();

        // Record Corrections handlers
        services.AddScoped<CorrectOperationsRecordHandler>();
        services.AddScoped<GetRecordCorrectionsListHandler>();

        // Demo Data services (P1-S02)
        // Register purger against the shared IOperationsDemoDataPurger and IDemoDataDomainPurger contracts so the
        // host DemoDataOrchestrator can resolve it (ADR-001, ADR-002).
        services.AddScoped<OperationsDemoDataPurger>();
        services.AddScoped<IOperationsDemoDataPurger, OperationsDemoDataPurger>();
        services.AddScoped<IDemoDataDomainPurger, OperationsDemoDataPurger>();
        services.AddScoped<IOperationsDemoDataSeeder, OperationsDemoDataSeeder>();

        // Cross-domain query services (ADR-001, ADR-003, TD-03)
        services.AddScoped<IOperationsCalendarQueryService, OperationsCalendarQueryService>();
        services.AddScoped<IOperationsModule, OperationsCalendarQueryService>();
        services.AddScoped<IKpiPeriodResolver, KpiPeriodResolver>();

        // KPI dashboard handler & executive summary provider (P2-S03)
        services.AddScoped<GetOperationsKpiDashboardHandler>();
        services.AddScoped<OperationsKpiSummaryProvider>();
        services.AddScoped<IDomainKpiSummaryProvider, OperationsKpiSummaryProvider>();

        // Configure policy-based authorization requirements (ADR-009)
        services.AddAuthorization(options =>
        {
            void AddPermissionPolicy(string permission)
            {
                options.AddPolicy(permission, policy => policy.RequireAssertion(ctx =>
                    ctx.User.Identity?.IsAuthenticated == true && (
                        ctx.User.HasClaim(c => (c.Type == "permission" || c.Type == "permissions") && (c.Value == permission || c.Value == "*"))
                        || ctx.User.IsInRole("Admin")
                        || ctx.User.IsInRole("OperationsAuthority"))));
            }

            AddPermissionPolicy(Permissions.Operations.Calendar.View);
            AddPermissionPolicy(Permissions.Operations.Calendar.Manage);
            AddPermissionPolicy(Permissions.Operations.Calendar.Approve);
            AddPermissionPolicy(Permissions.Operations.Calendar.Publish);
            AddPermissionPolicy(Permissions.Operations.Closure.Declare);
            AddPermissionPolicy(Permissions.Operations.Records.Correct);
        });

        return services;
    }
}
