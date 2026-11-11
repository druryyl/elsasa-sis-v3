namespace Elsasa.Sis.Students;

using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.AddAssessmentEvidence;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.DeleteAssessmentEvidence;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;
using Elsasa.Sis.Students.Features.Admission.Documents.DownloadApplicantDocument;
using Elsasa.Sis.Students.Features.Admission.Documents.UploadApplicantDocument;
using Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;
using Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationById;
using Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationsList;
using Elsasa.Sis.Students.Features.Admission.GetApplicationStatusByNumber;
using Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;
using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using Elsasa.Sis.Students.Features.Admission.StartApplicationReview;
using Elsasa.Sis.Students.Features.Admission.WithdrawAdmissionApplication;
using Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;
using Elsasa.Sis.Students.Features.Enrollment.Services;
using Elsasa.Sis.Students.Features.Records.Documents.DownloadStudentDocument;
using Elsasa.Sis.Students.Features.Records.Documents.GetStudentDocuments;
using Elsasa.Sis.Students.Features.Records.Documents.UploadStudentDocument;
using Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;
using Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;
using Elsasa.Sis.Students.Features.Records.Guardians.UpdateGuardianRelationship;
using Elsasa.Sis.Students.Features.Records.GetStudentById;
using Elsasa.Sis.Students.Features.Records.History.GetStudentAdministrativeHistory;
using Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;
using Elsasa.Sis.Students.Features.Records.SearchStudents;
using Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;
using Elsasa.Sis.Students.Features.Records.UpdateStudentIdentity;
using Elsasa.Sis.Students.Services;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Students.Features.DemoData;
using Elsasa.Sis.Students.Features.Kpis.GetStudentsKpiDashboard;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection registrations for the Students domain assembly (ADR-001).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all feature slice handlers, domain services, and authorization policies for the Students bounded context.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddStudentsDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Admission registration feature handler (P3-S04)
        services.AddScoped<RegisterApplicationHandler>();

        // Admission query feature handlers (P3-S05)
        services.AddScoped<GetAdmissionApplicationsListHandler>();
        services.AddScoped<GetAdmissionApplicationByIdHandler>();
        services.AddScoped<GetApplicationStatusByNumberHandler>();

        // Document management handlers (P4-S06)
        services.AddScoped<UploadApplicantDocumentHandler>();
        services.AddScoped<DownloadApplicantDocumentHandler>();
        services.AddScoped<VerifyApplicantDocumentHandler>();

        // Assessment evidence handlers (P4-S07)
        services.AddScoped<AddAssessmentEvidenceHandler>();
        services.AddScoped<UpdateAssessmentEvidenceHandler>();
        services.AddScoped<DeleteAssessmentEvidenceHandler>();

        // Admission review lifecycle & withdrawal handlers (P5-S08)
        services.AddScoped<StartApplicationReviewHandler>();
        services.AddScoped<WithdrawAdmissionApplicationHandler>();

        // Admission decision recording handler (P5-S09)
        services.AddScoped<RecordAdmissionDecisionHandler>();

        // Enrollment domain services & handlers (P2-S03, P3-S04)
        services.AddScoped<IStudentNumberGenerator, SqlStudentNumberGenerator>();
        services.AddScoped<EnrollAcceptedApplicantHandler>();

        // Cross-domain query services (ADR-003)
        services.AddScoped<IStudentsModule, StudentsModule>();

        // Student records query handlers (P4-S05, P4-S06)
        services.AddScoped<SearchStudentsHandler>();
        services.AddScoped<GetStudentByIdHandler>();
        services.AddScoped<GetStudentDocumentsHandler>();

        // Student records command handlers (P5-S07, P5-S08)
        services.AddScoped<UpdateStudentIdentityHandler>();
        services.AddScoped<AddGuardianRelationshipHandler>();
        services.AddScoped<UpdateGuardianRelationshipHandler>();

        // Student document lifecycle handlers (P6-S09)
        services.AddScoped<UploadStudentDocumentHandler>();
        services.AddScoped<DownloadStudentDocumentHandler>();
        services.AddScoped<VerifyStudentDocumentHandler>();

        // Student administrative placement, status, and history handlers (IS005 P2-S05, P3-S06, P3-S07)
        services.AddScoped<GetStudentAdministrativeHistoryHandler>();
        services.AddScoped<ChangeAdministrativePlacementHandler>();
        services.AddScoped<ChangeStudentStatusHandler>();

        // Students KPI vertical slice & executive summary provider (IS016 P2-S04)
        services.AddScoped<GetStudentsKpiDashboardHandler>();
        services.AddScoped<IDomainKpiSummaryProvider, StudentsKpiSummaryProvider>();

        // Configure policy-based authorization requirements (ADR-009)
        services.AddAuthorization(options =>
        {
            void AddPermissionPolicy(string permission, params string[] roles)
            {
                options.AddPolicy(permission, policy => policy.RequireAssertion(ctx =>
                    ctx.User.Identity?.IsAuthenticated == true && (
                        ctx.User.HasClaim(c => (c.Type == "permission" || c.Type == "permissions") && (c.Value == permission || c.Value == "*"))
                        || ctx.User.IsInRole("Admin")
                        || roles.Any(r => ctx.User.IsInRole(r)))));
            }

            AddPermissionPolicy(
                Permissions.Students.Admission.View,
                "AdmissionOfficer", "Admission Officer",
                "StudentAdminOfficer", "Student Administration Officer",
                "SchoolLeadership", "School Leadership");

            AddPermissionPolicy(
                Permissions.Students.Admission.Register,
                "AdmissionOfficer", "Admission Officer",
                "StudentAdminOfficer", "Student Administration Officer");

            AddPermissionPolicy(
                Permissions.Students.Admission.Review,
                "AdmissionOfficer", "Admission Officer");

            AddPermissionPolicy(
                Permissions.Students.Admission.VerifyDocument,
                "AdmissionOfficer", "Admission Officer");

            AddPermissionPolicy(
                Permissions.Students.Admission.Decide,
                "AdmissionOfficer", "Admission Officer",
                "SchoolLeadership", "School Leadership");

            AddPermissionPolicy(
                Permissions.Students.Enrollment.Execute,
                "StudentAdminOfficer", "Student Administration Officer",
                "AdmissionOfficer", "Admission Officer");

            AddPermissionPolicy(
                Permissions.Students.Records.View,
                "StudentAdminOfficer", "Student Administration Officer",
                "AdmissionOfficer", "Admission Officer",
                "SchoolLeadership", "School Leadership");

            AddPermissionPolicy(
                Permissions.Students.Records.Edit,
                "StudentAdminOfficer", "Student Administration Officer");
        });

        // Demo Data Purger & Seeder (P2-S05)
        // Register purger against the shared IDemoDataDomainPurger contract so the
        // host DemoDataOrchestrator can resolve it (ADR-001, ADR-002).
        services.AddScoped<StudentsDemoDataPurger>();
        services.AddScoped<IStudentsDemoDataPurger, StudentsDemoDataPurger>();
        services.AddScoped<IDemoDataDomainPurger, StudentsDemoDataPurger>();
        services.AddScoped<IStudentsDemoDataSeeder, StudentsDemoDataSeeder>();

        return services;
    }
}
