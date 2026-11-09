namespace Elsasa.Sis.Finance;

using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;
using Elsasa.Sis.Finance.Features.Accounts.SearchStudentFinancialAccounts;
using Elsasa.Sis.Finance.Features.DemoData;
using Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;
using Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;
using Elsasa.Sis.Finance.Features.Obligations.GetObligations;
using Elsasa.Sis.Finance.Features.Payers.CreatePayer;
using Elsasa.Sis.Finance.Features.Payers.GetPayerById;
using Elsasa.Sis.Finance.Features.Payers.GetPayers;
using Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;
using Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicies;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicyById;
using Elsasa.Sis.Finance.Features.Payments.ConfirmPayment;
using Elsasa.Sis.Finance.Features.Payments.GetPaymentById;
using Elsasa.Sis.Finance.Features.Payments.GetPaymentReceipt;
using Elsasa.Sis.Finance.Features.Payments.GetPayments;
using Elsasa.Sis.Finance.Features.Payments.RecordAndConfirmPayment;
using Elsasa.Sis.Finance.Features.Payments.RejectPayment;
using Elsasa.Sis.Finance.Features.Payments.SubmitPayment;
using Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;
using Elsasa.Sis.Finance.Features.Allocations.GetStudentReceivables;
using Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;
using Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;
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
        services.AddScoped<FinanceModule>();

        // Payer resolution & deduplication service (GAP-005, OQ-002, TD-03)
        services.AddScoped<IPayerResolver, PayerResolver>();
        services.AddScoped<PayerResolver>();

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

        // Demo Data services (P1-S04, P3-S06)
        // Register purger against the shared IDemoDataDomainPurger contract so the
        // host DemoDataOrchestrator can resolve it (ADR-001, ADR-002).
        services.AddScoped<FinanceDemoDataPurger>();
        services.AddScoped<IFinanceDemoDataPurger, FinanceDemoDataPurger>();
        services.AddScoped<IDemoDataDomainPurger, FinanceDemoDataPurger>();
        services.AddScoped<IFinancePolicyDemoDataSeeder, FinancePolicyDemoDataSeeder>();
        services.AddScoped<IFinanceTransactionsDemoDataSeeder, FinanceTransactionsDemoDataSeeder>();

        // Obligation Assessment feature handlers (P3-S06, P3-S07)
        services.AddScoped<BatchAssessObligationsHandler>();
        services.AddScoped<AssessAdHocObligationHandler>();
        services.AddScoped<GetObligationsHandler>();

        // Billing Document & Statement feature handlers (P3-S08)
        services.AddScoped<Features.Billing.GenerateBillingDocument.GenerateBillingDocumentHandler>();
        services.AddScoped<Features.Billing.GenerateStatement.GenerateStatementHandler>();
        services.AddScoped<Features.Billing.GetBillingDocuments.GetBillingDocumentsHandler>();
        services.AddScoped<Features.Billing.GetBillingDocumentById.GetBillingDocumentByIdHandler>();

        // Payer Management feature handlers (P3-S06, TD-03)
        services.AddScoped<GetPayersHandler>();
        services.AddScoped<CreatePayerHandler>();
        services.AddScoped<GetPayerByIdHandler>();

        // Payment Intake, Cashiering & Receipt Voucher feature handlers (P3-S07, TD-04, OQ-004)
        services.AddScoped<SubmitPaymentHandler>();
        services.AddScoped<RecordAndConfirmPaymentHandler>();
        services.AddScoped<ConfirmPaymentHandler>();
        services.AddScoped<RejectPaymentHandler>();
        services.AddScoped<GetPaymentsHandler>();
        services.AddScoped<GetPaymentByIdHandler>();
        services.AddScoped<GetPaymentReceiptHandler>();

        // Payment Allocation & Reallocation Engine feature handlers (P3-S08, P3-S09, TD-05, OQ-003)
        services.AddScoped<SuggestAllocationsHandler>();
        services.AddScoped<AllocatePaymentHandler>();
        services.AddScoped<ReallocatePaymentHandler>();
        services.AddScoped<GetStudentReceivablesHandler>();

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

            // Payments operational permissions
            AddPermissionPolicy(
                Permissions.Payments.View,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "SchoolLeadership", "School Leadership");

            AddPermissionPolicy(
                Permissions.Payments.Record,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "Admin");

            AddPermissionPolicy(
                Permissions.Payments.Confirm,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "Admin");

            AddPermissionPolicy(
                Permissions.Payments.Reject,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "Admin");

            // Allocations operational permissions
            AddPermissionPolicy(
                Permissions.Allocations.View,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "SchoolLeadership", "School Leadership");

            AddPermissionPolicy(
                Permissions.Allocations.Execute,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "Admin");

            AddPermissionPolicy(
                Permissions.Allocations.Reallocate,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "Admin");

            // Payers operational permissions
            AddPermissionPolicy(
                Permissions.Payers.View,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "SchoolLeadership", "School Leadership");

            AddPermissionPolicy(
                Permissions.Payers.Manage,
                "FinanceAuthority", "Finance Authority",
                "FinanceOfficer", "Finance Officer",
                "Admin");
        });

        return services;
    }
}
