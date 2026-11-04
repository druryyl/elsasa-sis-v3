namespace Elsasa.Sis.Finance;

using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;
using Elsasa.Sis.Finance.Features.Accounts.SearchStudentFinancialAccounts;
using Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;
using Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;
using Elsasa.Sis.Finance.Features.Obligations.GetObligations;
using Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;
using Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicies;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicyById;
using Elsasa.Sis.Finance.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection registrations for the Finance domain assembly (ADR-001).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Finance domain services exposed to other bounded contexts and internal feature slices.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for fluent chaining.</returns>
    public static IServiceCollection AddFinanceDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // FluentValidation validators scanning within Finance assembly (ADR-005)
        services.AddValidatorsFromAssembly(typeof(FinanceAssemblyMarker).Assembly);

        // Cross-domain query contract (ADR-003)
        services.AddScoped<IFinanceModule, FinanceModule>();

        // Student financial account lifecycle & provisioning services (GAP-002, TD-03)
        services.AddScoped<Features.Accounts.Services.IStudentAccountService, Features.Accounts.Services.StudentAccountService>();
        services.AddScoped<Features.Accounts.Services.StudentAccountService>();
        services.AddScoped<MediatR.INotificationHandler<Contracts.Students.Events.StudentEnrolledEvent>, Features.Accounts.Events.StudentEnrolledEventHandler>();
        services.AddScoped<Features.Accounts.Events.StudentEnrolledEventHandler>();

        // Accounts query handlers (P2-S05)
        services.AddScoped<GetStudentFinancialAccountHandler>();
        services.AddScoped<SearchStudentFinancialAccountsHandler>();

        // Financial Policy & Charge Rules feature handlers (GAP-006, GAP-007, TD-04, TD-06)
        services.AddScoped<CreateFinancialPolicyHandler>();
        services.AddScoped<ApproveFinancialPolicyHandler>();
        services.AddScoped<CreateChargeRuleHandler>();
        services.AddScoped<GetFinancialPoliciesHandler>();
        services.AddScoped<GetFinancialPolicyByIdHandler>();

        // Obligation Assessment feature handlers (P3-S06, P3-S07)
        services.AddScoped<BatchAssessObligationsHandler>();
        services.AddScoped<AssessAdHocObligationHandler>();
        services.AddScoped<GetObligationsHandler>();

        // Billing Document & Statement feature handlers (P3-S08)
        services.AddScoped<Features.Billing.GenerateBillingDocument.GenerateBillingDocumentHandler>();
        services.AddScoped<Features.Billing.GenerateStatement.GenerateStatementHandler>();
        services.AddScoped<Features.Billing.GetBillingDocuments.GetBillingDocumentsHandler>();
        services.AddScoped<Features.Billing.GetBillingDocumentById.GetBillingDocumentByIdHandler>();

        // Configure policy-based authorization requirements (GAP-008, TD-07, ADR-009)
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

            // Financial Policies governance permissions
            AddPermissionPolicy(
                Permissions.Policies.View,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "SchoolLeadership", "School Leadership");

            AddPermissionPolicy(
                Permissions.Policies.Manage,
                "FinanceAuthority", "Finance Authority");

            AddPermissionPolicy(
                Permissions.Policies.Approve,
                "FinanceAuthority", "Finance Authority");

            // Obligations operational permissions
            AddPermissionPolicy(
                Permissions.Obligations.View,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer");

            AddPermissionPolicy(
                Permissions.Obligations.Assess,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer");

            // Billing operational permissions
            AddPermissionPolicy(
                Permissions.Billing.View,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer");

            AddPermissionPolicy(
                Permissions.Billing.Generate,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer");

            // Accounts operational permissions
            AddPermissionPolicy(
                Permissions.Accounts.View,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer");
        });

        return services;
    }
}
