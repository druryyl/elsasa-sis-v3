namespace Elsasa.Sis.Teaching;

using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Teaching.Features.DemoData;
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

        // Synchronous cross-domain query contract service (ADR-001, ADR-003, ADR-010, TD-08, P5-S15)
        services.AddScoped<ITeachingModule, TeachingModuleImpl>();

        // Demo Data services (TD-09, P5-S16)
        services.AddScoped<TeachingDemoDataPurger>();
        services.AddScoped<ITeachingDemoDataPurger, TeachingDemoDataPurger>();
        services.AddScoped<IDemoDataDomainPurger, TeachingDemoDataPurger>();
        services.AddScoped<ITeachingDemoDataSeeder, TeachingDemoDataSeeder>();

        // FluentValidation validators in Teaching assembly
        services.AddValidatorsFromAssemblyContaining<TeachingAssemblyMarker>();

        // Configure policy-based authorization requirements (ADR-009, P1-S04)
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
        });

        return services;
    }
}
