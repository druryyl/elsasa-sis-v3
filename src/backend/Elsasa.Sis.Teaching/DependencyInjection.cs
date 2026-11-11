namespace Elsasa.Sis.Teaching;

using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Teaching.Features.DemoData;
using Elsasa.Sis.Teaching.Features.Kpis.GetTeachingKpiDashboard;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.GetQualificationTypes;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.RejectQualificationEvidence;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.SubmitQualificationEvidence;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.VerifyQualificationEvidence;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.ApproveTeacherProfile;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.ChangeTeacherStanding;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeacherProfileById;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeacherProfiles;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeachingRoles;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.RegisterTeacherProfile;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.UpdateTeacherProfile;
using Elsasa.Sis.Teaching.Features.TeachingCapabilities.AddTeachingCapability;
using Elsasa.Sis.Teaching.Features.TeachingCapabilities.GetCapabilityTypes;
using Elsasa.Sis.Teaching.Features.TeachingCapabilities.RemoveTeachingCapability;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.GrantTeachingAuthorization;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.UpdateTeachingAuthorizationStatus;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.AddSupervisionRequirement;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.ResolveSupervisionRequirement;
using Elsasa.Sis.Teaching.Features.RecordCorrections.ApplyRecordCorrection;
using Elsasa.Sis.Teaching.Features.RecordCorrections.ApproveRecordCorrection;
using Elsasa.Sis.Teaching.Features.RecordCorrections.GetRecordCorrections;
using Elsasa.Sis.Teaching.Features.TeachingDemands.CreateTeachingDemand;
using Elsasa.Sis.Teaching.Features.TeachingDemands.GetTeachingDemandById;
using Elsasa.Sis.Teaching.Features.TeachingDemands.GetTeachingDemands;
using Elsasa.Sis.Teaching.Features.TeacherEligibility.EvaluateCandidateEligibility;
using Elsasa.Sis.Teaching.Features.TeacherEligibility.GetCandidateEvaluations;
using Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemand;
using Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemandStatus;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.ProposeTeachingAssignment;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.ApproveTeachingAssignment;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.AcknowledgeTeachingAssignment;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.UpdateTeachingAssignmentStatus;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.GetTeachingAssignments;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.GetTeachingAssignmentById;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.GetAssignmentRoles;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.GetCapacityConflicts;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.ResolveCapacityConflict;
using Elsasa.Sis.Teaching.Features.Workload.CancelUnavailabilityWindow;
using Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow;
using Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability;
using Elsasa.Sis.Teaching.Features.Workload.GetTeacherAvailability;
using Elsasa.Sis.Teaching.Features.Workload.GetTeacherUnavailabilityWindows;
using Elsasa.Sis.Teaching.Features.Workload.GetWorkloadCommitments;
using Elsasa.Sis.Teaching.Features.Workload.ApproveWorkloadException;
using Elsasa.Sis.Teaching.Features.Workload.GetWorkloadExceptions;
using Elsasa.Sis.Teaching.Features.Workload.GetWorkloadPolicies;
using Elsasa.Sis.Teaching.Features.Workload.RecalculatePeriodWorkload;
using Elsasa.Sis.Teaching.Features.Workload.RequestWorkloadException;
using Elsasa.Sis.Teaching.Features.AssignmentCompletions.ConcludeTeachingAssignment;
using Elsasa.Sis.Teaching.Features.AssignmentCompletions.GetAssignmentCompletions;
using Elsasa.Sis.Teaching.Features.Substitutions.ArrangeEmergencySubstitution;
using Elsasa.Sis.Teaching.Features.Substitutions.ApproveSubstitutionException;
using Elsasa.Sis.Teaching.Features.Substitutions.GetTeachingSubstitutions;
using Elsasa.Sis.Teaching.Features.Substitutions.CancelSubstitution;
using Elsasa.Sis.Teaching.Features.Reassignments.ProposePermanentReassignment;
using Elsasa.Sis.Teaching.Features.Reassignments.ApprovePermanentReassignment;
using Elsasa.Sis.Teaching.Features.Reassignments.ConfirmReassignmentHandover;
using Elsasa.Sis.Teaching.Features.Reassignments.GetTeachingReassignments;
using Elsasa.Sis.Teaching.Features.Reassignments.GetTeachingReassignmentById;
using Elsasa.Sis.Teaching.Features.TeachingHistory.GetTeacherHistory;
using Elsasa.Sis.Teaching.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection registrations for the Teaching domain assembly (ADR-001, ADR-004, ADR-005, P1-S03).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all feature slice handlers, domain services, validators, and authorization policies for the Teaching bounded context.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Optional configuration root for binding options.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddTeachingModule(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Feature command slice handlers (P2-S05, P2-S06)
        services.AddScoped<RegisterTeacherProfileHandler>();
        services.AddScoped<UpdateTeacherProfileHandler>();
        services.AddScoped<ApproveTeacherProfileHandler>();
        services.AddScoped<ChangeTeacherStandingHandler>();

        // Feature query slice handlers (P2-S07)
        services.AddScoped<GetTeacherProfilesHandler>();
        services.AddScoped<GetTeacherProfileByIdHandler>();
        services.AddScoped<GetTeachingRolesHandler>();

        // Teaching capability slice handlers (P2-S08)
        services.AddScoped<AddTeachingCapabilityHandler>();
        services.AddScoped<RemoveTeachingCapabilityHandler>();
        services.AddScoped<GetCapabilityTypesHandler>();

        // Qualification evidence slice handlers (P3-S09, P3-S10)
        services.AddScoped<SubmitQualificationEvidenceHandler>();
        services.AddScoped<GetQualificationTypesHandler>();
        services.AddScoped<VerifyQualificationEvidenceHandler>();
        services.AddScoped<RejectQualificationEvidenceHandler>();

        // Teaching authorization and supervision slice handlers (P4-S11, P4-S12, P4-S13)
        services.AddScoped<GrantTeachingAuthorizationHandler>();
        services.AddScoped<UpdateTeachingAuthorizationStatusHandler>();
        services.AddScoped<AddSupervisionRequirementHandler>();
        services.AddScoped<ResolveSupervisionRequirementHandler>();

        // Record corrections slice handlers (TD-06, P5-S14)
        services.AddScoped<ApplyRecordCorrectionHandler>();
        services.AddScoped<ApproveRecordCorrectionHandler>();
        services.AddScoped<GetRecordCorrectionsHandler>();

        // Teaching Demands slice handlers (TD-02, P2-S04)
        services.AddScoped<CreateTeachingDemandHandler>();
        services.AddScoped<UpdateTeachingDemandHandler>();
        services.AddScoped<UpdateTeachingDemandStatusHandler>();
        services.AddScoped<GetTeachingDemandsHandler>();
        services.AddScoped<GetTeachingDemandByIdHandler>();

        // Candidate Eligibility Engine slice handlers (TD-03, P2-S05)
        services.AddScoped<EvaluateCandidateEligibilityHandler>();
        services.AddScoped<GetCandidateEvaluationsHandler>();

        // Teaching Assignments Proposal, Approval & Lifecycle handlers (TD-01, TD-04, P3-S06)
        services.AddScoped<ProposeTeachingAssignmentHandler>();
        services.AddScoped<ApproveTeachingAssignmentHandler>();
        services.AddScoped<AcknowledgeTeachingAssignmentHandler>();
        services.AddScoped<UpdateTeachingAssignmentStatusHandler>();
        services.AddScoped<GetTeachingAssignmentsHandler>();
        services.AddScoped<GetTeachingAssignmentByIdHandler>();
        services.AddScoped<GetAssignmentRolesHandler>();
        services.AddScoped<GetCapacityConflictsHandler>();
        services.AddScoped<ResolveCapacityConflictHandler>();

        // Teacher Availability & Unavailability slice handlers (TD-04, P3-S08)
        services.AddScoped<DeclareTeacherAvailabilityHandler>();
        services.AddScoped<GetTeacherAvailabilityHandler>();
        services.AddScoped<CreateUnavailabilityWindowHandler>();
        services.AddScoped<CancelUnavailabilityWindowHandler>();
        services.AddScoped<GetTeacherUnavailabilityWindowsHandler>();

        // Workload Policies, Commitments & Recalculation slice handlers (TD-04, P3-S09)
        services.AddScoped<GetWorkloadPoliciesHandler>();
        services.AddScoped<GetWorkloadCommitmentsHandler>();
        services.AddScoped<RecalculatePeriodWorkloadHandler>();

        // Workload Exceptions Governance & Approval slice handlers (TD-04, P3-S10)
        services.AddScoped<RequestWorkloadExceptionHandler>();
        services.AddScoped<ApproveWorkloadExceptionHandler>();
        services.AddScoped<GetWorkloadExceptionsHandler>();

        // Assignment Conclusions & Capacity Release slice handlers (TD-01, TD-04, P4-S13)
        services.AddScoped<ConcludeTeachingAssignmentHandler>();
        services.AddScoped<GetAssignmentCompletionsHandler>();

        // Emergency Substitutions & Pre-Authorized Pool Delegation slice handlers (TD-05, P4-S11)
        services.AddScoped<ArrangeEmergencySubstitutionHandler>();
        services.AddScoped<ApproveSubstitutionExceptionHandler>();
        services.AddScoped<GetTeachingSubstitutionsHandler>();
        services.AddScoped<CancelSubstitutionHandler>();

        // Permanent Reassignments & Handover Management slice handlers (TD-01, TD-04, P4-S12)
        services.AddScoped<ProposePermanentReassignmentHandler>();
        services.AddScoped<ApprovePermanentReassignmentHandler>();
        services.AddScoped<ConfirmReassignmentHandoverHandler>();
        services.AddScoped<GetTeachingReassignmentsHandler>();
        services.AddScoped<GetTeachingReassignmentByIdHandler>();

        // Teaching History Append-Only Audit Log slice handlers (TD-07, P4-S14)
        services.AddScoped<GetTeacherHistoryHandler>();

        // KPI Dashboard slice handler & Executive Summary provider (CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS, P3-S07)
        services.AddScoped<GetTeachingKpiDashboardHandler>();
        services.AddScoped<IDomainKpiSummaryProvider, TeachingKpiSummaryProvider>();

        // Synchronous cross-domain query contract service (ADR-001, ADR-003, ADR-010, TD-08, P5-S15)
        services.AddScoped<ITeachingModule, TeachingModuleImpl>();

        // Demo Data services (TD-09, P5-S16)
        services.AddScoped<TeachingDemoDataPurger>();
        services.AddScoped<ITeachingDemoDataPurger, TeachingDemoDataPurger>();
        services.AddScoped<IDemoDataDomainPurger, TeachingDemoDataPurger>();
        services.AddScoped<ITeachingDemoDataSeeder, TeachingDemoDataSeeder>();
        services.AddScoped<ITeachingAssignmentsDemoDataSeeder, TeachingAssignmentsDemoDataSeeder>();

        // FluentValidation validators in Teaching assembly
        services.AddValidatorsFromAssemblyContaining<TeachingAssemblyMarker>();

        // Configure policy-based authorization requirements (ADR-009, P1-S04, P1-S03)
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

            // Permissions.Teaching.Faculty.View: Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Faculty.View,
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Permissions.Teaching.Faculty.Manage: Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Faculty.Manage,
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Permissions.Teaching.Faculty.Verify: Qualification Verifier, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Faculty.Verify,
                "QualificationVerifier", "Qualification Verifier",
                "TeachingAuthority", "Teaching Authority");

            // Permissions.Teaching.Faculty.Authorize: Teaching Authority, Admin (Kepala Sekolah / VP Academics)
            AddPermissionPolicy(
                Permissions.Teaching.Faculty.Authorize,
                "TeachingAuthority", "Teaching Authority",
                "KepalaSekolah", "Kepala Sekolah",
                "VPAcademics", "VP Academics");

            // Permissions.Teaching.Faculty.Correct: Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Faculty.Correct,
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Stage 2: Permissions.Teaching.Assignments.View: Teacher, Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Assignments.View,
                "Teacher",
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Stage 2: Permissions.Teaching.Assignments.Manage: Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Assignments.Manage,
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Stage 2: Permissions.Teaching.Assignments.Approve: Teaching Authority, Admin (Kepala Sekolah / VP Academics)
            AddPermissionPolicy(
                Permissions.Teaching.Assignments.Approve,
                "TeachingAuthority", "Teaching Authority",
                "KepalaSekolah", "Kepala Sekolah",
                "VPAcademics", "VP Academics");

            // Stage 2: Permissions.Teaching.Substitutions.Arrange: Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Substitutions.Arrange,
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Stage 2: Permissions.Teaching.Substitutions.ApproveException: Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Substitutions.ApproveException,
                "TeachingAuthority", "Teaching Authority",
                "KepalaSekolah", "Kepala Sekolah",
                "VPAcademics", "VP Academics");

            // Stage 2: Permissions.Teaching.Workload.View: Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Workload.View,
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Stage 2: Permissions.Teaching.Workload.Recalculate: Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Workload.Recalculate,
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Stage 2: Permissions.Teaching.Workload.RequestException: Teaching Coordinator, Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Workload.RequestException,
                "TeachingCoordinator", "Teaching Coordinator",
                "TeachingAuthority", "Teaching Authority");

            // Stage 2: Permissions.Teaching.Workload.ApproveException: Teaching Authority, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Workload.ApproveException,
                "TeachingAuthority", "Teaching Authority",
                "KepalaSekolah", "Kepala Sekolah",
                "VPAcademics", "VP Academics");

            // Stage 2: Permissions.Teaching.Availability.Declare: Teacher, Teaching Coordinator, Admin
            AddPermissionPolicy(
                Permissions.Teaching.Availability.Declare,
                "Teacher",
                "TeachingCoordinator", "Teaching Coordinator");

            // Level 2 Teaching KPI Dashboard policy (TD-05, P3-S07)
            options.AddPolicy(TeachingKpiPolicies.DashboardView, policy => policy.RequireAssertion(ctx =>
                ctx.User.Identity?.IsAuthenticated == true && (
                    ctx.User.HasClaim(c => (c.Type == "permission" || c.Type == "permissions")
                        && (c.Value == Permissions.Teaching.Faculty.View
                            || c.Value == Permissions.Teaching.Workload.View
                            || c.Value == "*"))
                    || ctx.User.IsInRole("Admin")
                    || ctx.User.IsInRole("SuperAdmin")
                    || ctx.User.IsInRole("AcademicCoordinator")
                    || ctx.User.IsInRole("AcademicAuthority")
                    || ctx.User.IsInRole("TeachingAuthority")
                    || ctx.User.IsInRole("Teaching Authority")
                    || ctx.User.IsInRole("TeachingCoordinator")
                    || ctx.User.IsInRole("Teaching Coordinator")
                    || ctx.User.IsInRole("Teacher"))));
        });

        return services;
    }
}
