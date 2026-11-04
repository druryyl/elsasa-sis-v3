namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Finance;
using Elsasa.Sis.Finance.Endpoints;
using Elsasa.Sis.Finance.Features.Accounts.Events;
using Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;
using Elsasa.Sis.Finance.Features.Accounts.SearchStudentFinancialAccounts;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;
using Elsasa.Sis.Finance.Features.Billing.GenerateStatement;
using Elsasa.Sis.Finance.Features.Billing.GetBillingDocumentById;
using Elsasa.Sis.Finance.Features.Billing.GetBillingDocuments;
using Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;
using Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;
using Elsasa.Sis.Finance.Features.Obligations.GetObligations;
using Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;
using Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicies;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicyById;
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

        // Assert - Obligation Assessment Handlers
        Assert.NotNull(provider.GetService<BatchAssessObligationsHandler>());
        Assert.NotNull(provider.GetService<AssessAdHocObligationHandler>());
        Assert.NotNull(provider.GetService<GetObligationsHandler>());

        // Assert - Billing Document Handlers
        Assert.NotNull(provider.GetService<GenerateBillingDocumentHandler>());
        Assert.NotNull(provider.GetService<GenerateStatementHandler>());
        Assert.NotNull(provider.GetService<GetBillingDocumentsHandler>());
        Assert.NotNull(provider.GetService<GetBillingDocumentByIdHandler>());
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

        // Assert
        Assert.NotNull(provider.GetService<IValidator<CreateFinancialPolicyRequest>>());
        Assert.NotNull(provider.GetService<IValidator<ApproveFinancialPolicyRequest>>());
        Assert.NotNull(provider.GetService<IValidator<CreateChargeRuleRequest>>());
        Assert.NotNull(provider.GetService<IValidator<BatchAssessObligationsRequest>>());
        Assert.NotNull(provider.GetService<IValidator<AssessAdHocObligationRequest>>());
        Assert.NotNull(provider.GetService<IValidator<GenerateBillingDocumentRequest>>());
        Assert.NotNull(provider.GetService<IValidator<GenerateStatementRequest>>());
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

        // Assert - All Permissions.Finance.* policies must be configured
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
