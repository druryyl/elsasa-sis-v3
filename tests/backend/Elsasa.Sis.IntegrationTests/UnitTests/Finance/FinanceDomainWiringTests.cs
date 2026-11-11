namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Finance;
using Elsasa.Sis.Finance.Endpoints;
using Elsasa.Sis.Finance.Features.Accounts.Events;
using Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;
using Elsasa.Sis.Finance.Features.Accounts.SearchStudentFinancialAccounts;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;
using Elsasa.Sis.Finance.Features.Allocations.GetStudentReceivables;
using Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;
using Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;
using Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;
using Elsasa.Sis.Finance.Features.Billing.GenerateStatement;
using Elsasa.Sis.Finance.Features.Billing.GetBillingDocumentById;
using Elsasa.Sis.Finance.Features.Billing.GetBillingDocuments;
using Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;
using Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;
using Elsasa.Sis.Finance.Features.Obligations.GetObligations;
using Elsasa.Sis.Finance.Features.Payers.CreatePayer;
using Elsasa.Sis.Finance.Features.Payers.GetPayerById;
using Elsasa.Sis.Finance.Features.Payers.GetPayers;
using Elsasa.Sis.Finance.Features.Payments.ConfirmPayment;
using Elsasa.Sis.Finance.Features.Payments.GetPaymentById;
using Elsasa.Sis.Finance.Features.Payments.GetPaymentReceipt;
using Elsasa.Sis.Finance.Features.Payments.GetPayments;
using Elsasa.Sis.Finance.Features.Payments.RecordAndConfirmPayment;
using Elsasa.Sis.Finance.Features.Payments.RejectPayment;
using Elsasa.Sis.Finance.Features.Payments.SubmitPayment;
using Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;
using Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicies;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicyById;
using Elsasa.Sis.Finance.Features.Policies.GetPolicyCategories;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Endpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public sealed class FinanceDomainWiringTests
{
    private sealed class DummyCurrentUser : ICurrentUser
    {
        public Guid? Id { get; } = Guid.NewGuid();
        public string? Username => "test.user";
        public string? Email => "test.user@elsasa.edu";
        public IReadOnlyList<string> Roles => ["FinanceAuthority", "Admin"];
        public IReadOnlyList<string> Permissions => ["*"];
        public bool IsAuthenticated => true;
    }

    [Fact]
    public void AddFinanceDomain_Should_RegisterAllFeatureHandlersAndServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(FinanceAssemblyMarker).Assembly));
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;"));
        services.AddSingleton<ICurrentUser>(new DummyCurrentUser());

        // Act
        services.AddFinanceDomain();
        var provider = services.BuildServiceProvider();

        // Assert - Domain Module
        Assert.NotNull(provider.GetService<IFinanceModule>());

        // Assert - Payer Resolver Service (P2-S03, GAP-005, TD-03)
        Assert.NotNull(provider.GetService<IPayerResolver>());
        Assert.NotNull(provider.GetService<PayerResolver>());

        // Assert - Account Lifecycle & Provisioning Services
        Assert.NotNull(provider.GetService<IStudentAccountService>());
        Assert.NotNull(provider.GetService<INotificationHandler<StudentEnrolledEvent>>());

        // Assert - Account Query Handlers
        Assert.NotNull(provider.GetService<GetStudentFinancialAccountHandler>());
        Assert.NotNull(provider.GetService<SearchStudentFinancialAccountsHandler>());

        // Assert - Policy Governance Handlers
        Assert.NotNull(provider.GetService<CreateFinancialPolicyHandler>());
        Assert.NotNull(provider.GetService<ApproveFinancialPolicyHandler>());
        Assert.NotNull(provider.GetService<CreateChargeRuleHandler>());
        Assert.NotNull(provider.GetService<GetFinancialPoliciesHandler>());
        Assert.NotNull(provider.GetService<GetFinancialPolicyByIdHandler>());
        Assert.NotNull(provider.GetService<GetPolicyCategoriesHandler>());

        // Assert - Obligation Assessment Handlers
        Assert.NotNull(provider.GetService<BatchAssessObligationsHandler>());
        Assert.NotNull(provider.GetService<AssessAdHocObligationHandler>());
        Assert.NotNull(provider.GetService<GetObligationsHandler>());

        // Assert - Billing Document Handlers
        Assert.NotNull(provider.GetService<GenerateBillingDocumentHandler>());
        Assert.NotNull(provider.GetService<GenerateStatementHandler>());
        Assert.NotNull(provider.GetService<GetBillingDocumentsHandler>());
        Assert.NotNull(provider.GetService<GetBillingDocumentByIdHandler>());

        // Assert - Payer Management Handlers (P3-S06)
        Assert.NotNull(provider.GetService<GetPayersHandler>());
        Assert.NotNull(provider.GetService<CreatePayerHandler>());
        Assert.NotNull(provider.GetService<GetPayerByIdHandler>());

        // Assert - Payment Intake & Cashiering Handlers (P3-S07)
        Assert.NotNull(provider.GetService<SubmitPaymentHandler>());
        Assert.NotNull(provider.GetService<RecordAndConfirmPaymentHandler>());
        Assert.NotNull(provider.GetService<ConfirmPaymentHandler>());
        Assert.NotNull(provider.GetService<RejectPaymentHandler>());
        Assert.NotNull(provider.GetService<GetPaymentsHandler>());
        Assert.NotNull(provider.GetService<GetPaymentByIdHandler>());
        Assert.NotNull(provider.GetService<GetPaymentReceiptHandler>());

        // Assert - Allocation & Receivables Handlers (P3-S08, P3-S09)
        Assert.NotNull(provider.GetService<SuggestAllocationsHandler>());
        Assert.NotNull(provider.GetService<AllocatePaymentHandler>());
        Assert.NotNull(provider.GetService<ReallocatePaymentHandler>());
        Assert.NotNull(provider.GetService<GetStudentReceivablesHandler>());
    }

    [Fact]
    public void AddFinanceDomain_Should_RegisterFluentValidationValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;"));
        services.AddSingleton<ICurrentUser>(new DummyCurrentUser());

        // Act
        services.AddFinanceDomain();
        var provider = services.BuildServiceProvider();

        // Assert - Submodule 1 & 2 Validators
        Assert.NotNull(provider.GetService<IValidator<CreateFinancialPolicyRequest>>());
        Assert.NotNull(provider.GetService<IValidator<ApproveFinancialPolicyRequest>>());
        Assert.NotNull(provider.GetService<IValidator<CreateChargeRuleRequest>>());
        Assert.NotNull(provider.GetService<IValidator<BatchAssessObligationsRequest>>());
        Assert.NotNull(provider.GetService<IValidator<AssessAdHocObligationRequest>>());
        Assert.NotNull(provider.GetService<IValidator<GenerateBillingDocumentRequest>>());
        Assert.NotNull(provider.GetService<IValidator<GenerateStatementRequest>>());

        // Assert - Submodule 3 Validators (P3-S06, P3-S07, P3-S08)
        Assert.NotNull(provider.GetService<IValidator<CreatePayerRequest>>());
        Assert.NotNull(provider.GetService<IValidator<SubmitPaymentRequest>>());
        Assert.NotNull(provider.GetService<IValidator<RecordAndConfirmPaymentRequest>>());
        Assert.NotNull(provider.GetService<IValidator<RejectPaymentRequest>>());
        Assert.NotNull(provider.GetService<IValidator<SuggestAllocationsRequest>>());
        Assert.NotNull(provider.GetService<IValidator<AllocatePaymentRequest>>());
        Assert.NotNull(provider.GetService<IValidator<ReallocatePaymentRequest>>());
    }

    [Fact]
    public void AddFinanceDomain_Should_ConfigureAllDomainAuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;"));
        services.AddSingleton<ICurrentUser>(new DummyCurrentUser());

        // Act
        services.AddFinanceDomain();
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // Assert - All authoritative permissions policies must be configured (TD-07, GAP-008)
        string[] requiredPolicies =
        [
            Permissions.Finance.Policies.View,
            Permissions.Finance.Policies.Manage,
            Permissions.Finance.Policies.Approve,
            Permissions.Finance.Obligations.View,
            Permissions.Finance.Obligations.Assess,
            Permissions.Finance.Billing.View,
            Permissions.Finance.Billing.Generate,
            Permissions.Finance.Accounts.View,
            Permissions.Finance.Payments.View,
            Permissions.Finance.Payments.Record,
            Permissions.Finance.Payments.Confirm,
            Permissions.Finance.Payments.Reject,
            Permissions.Finance.Allocations.View,
            Permissions.Finance.Allocations.Execute,
            Permissions.Finance.Allocations.Reallocate,
            Permissions.Finance.Payers.View,
            Permissions.Finance.Payers.Manage,
        ];

        foreach (var policyName in requiredPolicies)
        {
            var policy = authOptions.GetPolicy(policyName);
            Assert.NotNull(policy);
            Assert.NotEmpty(policy.Requirements);
        }
    }

    [Fact]
    public void FinanceEndpoints_Should_ImplementIEndpoint_And_MapCleanly()
    {
        // Arrange
        var endpoint = new FinanceEndpoints();

        // Assert
        Assert.IsAssignableFrom<IEndpoint>(endpoint);

        var services = new ServiceCollection();
        services.AddRouting();
        services.AddAuthorization();
        var provider = services.BuildServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(provider);

        // Act & Assert (Should not throw)
        endpoint.MapEndpoint(builder);
    }

    [Fact]
    public void MapEndpoints_Should_DiscoverAndMapAllFinanceEndpoints()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRouting();
        services.AddAuthorization();
        var provider = services.BuildServiceProvider();
        var builder = new DefaultEndpointRouteBuilder(provider);

        // Act & Assert - Map all endpoints from Finance assembly
        builder.MapEndpoints(typeof(FinanceAssemblyMarker).Assembly);

        // Assert - Specific endpoints verified
        Assert.IsAssignableFrom<IEndpoint>(new GetPolicyCategoriesEndpoint());
        Assert.IsAssignableFrom<IEndpoint>(new GetStudentReceivablesEndpoint());
    }

    private sealed class DefaultEndpointRouteBuilder(IServiceProvider serviceProvider) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return new ApplicationBuilder(ServiceProvider);
        }
    }
}
