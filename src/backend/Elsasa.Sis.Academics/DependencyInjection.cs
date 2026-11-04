namespace Elsasa.Sis.Academics;

using Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;
using Elsasa.Sis.Academics.Features.AcademicRequirements.DeleteAcademicRequirement;
using Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirementById;
using Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirements;
using Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;
using Elsasa.Sis.Academics.Features.LearningStructures.ApproveLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.ArchiveLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;
using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructures;
using Elsasa.Sis.Academics.Features.LearningStructures.SubmitLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.SupersedeLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;
using Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;
using Elsasa.Sis.Academics.Features.LearningUnits.DeleteLearningUnit;
using Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnitById;
using Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnits;
using Elsasa.Sis.Academics.Features.LearningUnits.UpdateLearningUnit;
using Elsasa.Sis.Academics.Services;
using Elsasa.Sis.Contracts.Academics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection registrations for the Academics domain assembly (ADR-001, ADR-004, ADR-005, P3-S10).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all feature slice handlers, domain services, validators, and authorization policies for the Academics bounded context.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddAcademicsDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Learning Structures feature handlers (P3-S06)
        services.AddScoped<CreateLearningStructureHandler>();
        services.AddScoped<GetLearningStructuresHandler>();
        services.AddScoped<GetLearningStructureByIdHandler>();
        services.AddScoped<UpdateLearningStructureHandler>();

        // Learning Structures governance lifecycle handlers (P3-S07)
        services.AddScoped<SubmitLearningStructureHandler>();
        services.AddScoped<ApproveLearningStructureHandler>();
        services.AddScoped<SupersedeLearningStructureHandler>();
        services.AddScoped<ArchiveLearningStructureHandler>();

        // Learning Units feature handlers (P3-S08)
        services.AddScoped<CreateLearningUnitHandler>();
        services.AddScoped<GetLearningUnitsHandler>();
        services.AddScoped<GetLearningUnitByIdHandler>();
        services.AddScoped<UpdateLearningUnitHandler>();
        services.AddScoped<DeleteLearningUnitHandler>();

        // Academic Requirements feature handlers (P3-S09)
        services.AddScoped<CreateAcademicRequirementHandler>();
        services.AddScoped<GetAcademicRequirementsHandler>();
        services.AddScoped<GetAcademicRequirementByIdHandler>();
        services.AddScoped<UpdateAcademicRequirementHandler>();
        services.AddScoped<DeleteAcademicRequirementHandler>();

        // Cross-domain query & cloning services (P2-S04, P2-S05)
        services.AddScoped<IAcademicsModule, AcademicsModule>();
        services.AddScoped<ICurriculumCloningService, CurriculumCloningService>();

        // FluentValidation validators in Academics assembly
        services.AddValidatorsFromAssemblyContaining<AcademicsAssemblyMarker>();

        // Configure policy-based authorization requirements (ADR-009, TD-07)
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

            // Curriculum permissions (ADR-009, IS008 Section 9.1)
            // Academic Coordinator: View, Manage, Submit
            // Academic Authority: View, Approve, Archive (and Manage/Supersede)
            // Admin: All
            AddPermissionPolicy(
                Permissions.Academics.Curriculum.View,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Curriculum.Manage,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Curriculum.Submit,
                "AcademicCoordinator", "Academic Coordinator",
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Curriculum.Approve,
                "AcademicAuthority", "Academic Authority");

            AddPermissionPolicy(
                Permissions.Academics.Curriculum.Archive,
                "AcademicAuthority", "Academic Authority");
        });

        return services;
    }
}
