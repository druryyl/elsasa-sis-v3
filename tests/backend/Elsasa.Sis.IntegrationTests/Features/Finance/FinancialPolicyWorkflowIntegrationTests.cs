namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Finance;
using Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;
using Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicies;
using Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicyById;
using Elsasa.Sis.Finance.Features.Policies.GetPolicyCategories;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>
/// Integration tests verifying financial policy lifecycle and charge rule governance (P4-S12, P2-S04, GAP-007, TD-06, ADR-009).
/// Verifies policy creation in draft status, separation-of-duties approval authorization, and charge rule attachment.
/// </summary>
public sealed class FinancialPolicyWorkflowIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [SkippableFact]
    public async Task FinancialPolicyWorkflow_Should_CreateDraft_ApproveAsFinanceAuthority_And_AttachChargeRule()
    {
        SkipIfDatabaseUnavailable();

        var policyNumber = Random.Shared.Next(100, 999);
        var policyCode = $"FP-2026-{policyNumber}";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Create financial policy in draft status
        var createPolicyRequest = new CreateFinancialPolicyRequest(
            PolicyCode: policyCode,
            Name: "Kebijakan Biaya Operasional 2026",
            Category: "tuition",
            BusinessPurpose: "Menetapkan pedoman biaya operasional pendidikan tahun ajaran 2026/2027.",
            ApplicableContext: "All",
            EffectiveDate: today,
            EndDate: today.AddYears(1),
            SupersedesPolicyId: null,
            Notes: "Initial draft creation for integration test.");

        var createPolicyResponse = await Client.PostAsJsonAsync("/api/finance/policies", createPolicyRequest);
        createPolicyResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdPolicy = await createPolicyResponse.Content.ReadFromJsonAsync<CreateFinancialPolicyResponse>();
        createdPolicy.Should().NotBeNull();
        createdPolicy!.PolicyCode.Should().Be(policyCode);
        createdPolicy.Status.Should().Be("draft");

        // 2. Verify separation of duties: Finance Officer without Approve permission is Forbidden (403)
        Factory.CurrentUser.SetUser(
            id: Guid.NewGuid(),
            username: "finance-officer",
            permissions: [Permissions.Finance.Policies.Manage, Permissions.Finance.Policies.View]);

        var forbiddenApproveResponse = await Client.PostAsJsonAsync(
            $"/api/finance/policies/{createdPolicy.Id}/approve",
            new ApproveFinancialPolicyRequest("Direktur Keuangan", "Approval attempt"));
        forbiddenApproveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Approve policy as Finance Authority -> status transitions to active
        Factory.CurrentUser.SetUser(
            id: Guid.NewGuid(),
            username: "finance-authority",
            permissions: [Permissions.Finance.Policies.Approve, Permissions.Finance.Policies.Manage, Permissions.Finance.Policies.View]);

        var approveRequest = new ApproveFinancialPolicyRequest(
            ApprovingAuthority: "Kepala Yayasan / Direktur Keuangan",
            Notes: "Disetujui untuk tahun ajaran 2026/2027");

        var approveResponse = await Client.PostAsJsonAsync($"/api/finance/policies/{createdPolicy.Id}/approve", approveRequest);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedPolicy = await approveResponse.Content.ReadFromJsonAsync<ApproveFinancialPolicyResponse>();
        approvedPolicy.Should().NotBeNull();
        approvedPolicy!.Status.Should().Be("active");
        approvedPolicy.ApprovingAuthority.Should().Be("Kepala Yayasan / Direktur Keuangan");
        approvedPolicy.ApprovedDate.Should().Be(today);

        Factory.CurrentUser.Reset();

        // 4. Create charge rule attached to the active policy
        var ruleCode = $"CR-2026-TEST-{policyNumber}";
        var createRuleRequest = new CreateChargeRuleRequest(
            RuleCode: ruleCode,
            Name: "Uang Sarana dan Prasarana",
            ChargeType: "facility_fee",
            IsMandatory: true,
            DeterminationMethod: "fixed_amount",
            BaseAmount: 2500000,
            CurrencyCode: "IDR",
            RecurrencePattern: "one_time",
            DueRuleType: "days_after_issuance",
            DueRuleValue: "30",
            ApplicableGrade: null,
            ApplicableProgram: null,
            Notes: "Charge rule attached to approved policy");

        var createRuleResponse = await Client.PostAsJsonAsync($"/api/finance/policies/{createdPolicy.Id}/rules", createRuleRequest);
        createRuleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdRule = await createRuleResponse.Content.ReadFromJsonAsync<CreateChargeRuleResponse>();
        createdRule.Should().NotBeNull();
        createdRule!.RuleCode.Should().Be(ruleCode);
        createdRule.PolicyId.Should().Be(createdPolicy.Id);
        createdRule.BaseAmount.Should().Be(2500000);
        createdRule.Status.Should().Be("active");

        // 5. Query detailed policy by ID and verify nested charge rules list
        var getByIdResponse = await Client.GetAsync($"/api/finance/policies/{createdPolicy.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var policyDetail = await getByIdResponse.Content.ReadFromJsonAsync<FinancialPolicyDetailResponse>();
        policyDetail.Should().NotBeNull();
        policyDetail!.Id.Should().Be(createdPolicy.Id);
        policyDetail.Status.Should().Be("active");
        policyDetail.ApprovingAuthority.Should().Be("Kepala Yayasan / Direktur Keuangan");
        policyDetail.Rules.Should().ContainSingle(r => r.RuleCode == ruleCode && r.BaseAmount == 2500000);

        // 6. Query policies list with status filter
        var listResponse = await Client.GetAsync("/api/finance/policies?status=active");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<List<FinancialPolicyListItemDto>>();
        list.Should().NotBeNull();
        list!.Should().Contain(p => p.Id == createdPolicy.Id && p.Status == "active");
    }

    [SkippableFact]
    public async Task CreateChargeRule_OnDraftPolicy_Should_ReturnConflictOrValidationFailure()
    {
        SkipIfDatabaseUnavailable();

        var policyNumber = Random.Shared.Next(100, 999);
        var policyCode = $"FP-2026-{policyNumber}";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create policy in draft
        var createPolicyRequest = new CreateFinancialPolicyRequest(
            PolicyCode: policyCode,
            Name: "Kebijakan Draft",
            Category: "general_fee",
            BusinessPurpose: "Tujuan draft",
            ApplicableContext: "All",
            EffectiveDate: today,
            EndDate: today.AddYears(1),
            SupersedesPolicyId: null,
            Notes: null);

        var createPolicyResponse = await Client.PostAsJsonAsync("/api/finance/policies", createPolicyRequest);
        createPolicyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var policy = await createPolicyResponse.Content.ReadFromJsonAsync<CreateFinancialPolicyResponse>();

        // Attempt to attach rule to draft policy
        var createRuleRequest = new CreateChargeRuleRequest(
            RuleCode: $"CR-DRAFT-{policyNumber}",
            Name: "Rule On Draft",
            ChargeType: "tuition_fee",
            IsMandatory: true,
            DeterminationMethod: "fixed_amount",
            BaseAmount: 1000000,
            CurrencyCode: "IDR",
            RecurrencePattern: "monthly",
            DueRuleType: "days_after_issuance",
            DueRuleValue: "10",
            ApplicableGrade: null,
            ApplicableProgram: null,
            Notes: null);

        var ruleResponse = await Client.PostAsJsonAsync($"/api/finance/policies/{policy!.Id}/rules", createRuleRequest);
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict, "charge rules can only be attached to active policies");
    }

    [SkippableFact]
    public async Task GetPolicyCategories_Should_EnforceAuthorization_And_ReturnSeededActiveCategoriesInDisplayOrder()
    {
        SkipIfDatabaseUnavailable();

        // 1. Unauthenticated request -> 401 Unauthorized
        Factory.CurrentUser.SetUnauthenticated();
        var unauthenticatedResponse = await Client.GetAsync("/api/finance/policy-categories");
        unauthenticatedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Authenticated user lacking Finance.Policies.View permission -> 403 Forbidden
        Factory.CurrentUser.SetUser(
            id: Guid.NewGuid(),
            username: "unauthorized-user",
            permissions: []);
        var forbiddenResponse = await Client.GetAsync("/api/finance/policy-categories");
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Authorized user with Finance.Policies.View -> 200 OK with 8 seeded active categories ordered by display_order
        Factory.CurrentUser.SetUser(
            id: Guid.NewGuid(),
            username: "finance-viewer",
            permissions: [Permissions.Finance.Policies.View]);

        var response = await Client.GetAsync("/api/finance/policy-categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await response.Content.ReadFromJsonAsync<List<PolicyCategoryDto>>();
        Assert.NotNull(categories);
        categories.Should().HaveCount(8);
        categories.Should().OnlyContain(c => c.IsActive);
        categories.Select(c => c.Code).Should().ContainInOrder(
            "tuition",
            "admission_fee",
            "facilities",
            "activities",
            "general_fee",
            "scholarship_guideline",
            "refund_rule",
            "clearance_rule");
        categories.Select(c => c.DisplayOrder).Should().BeInAscendingOrder();
    }

    [SkippableFact]
    public async Task CreateFinancialPolicy_WithUnknownOrInactiveCategory_Should_ReturnInvalidCategoryValidationError()
    {
        SkipIfDatabaseUnavailable();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var unknownPolicySuffix = Random.Shared.Next(100, 499);
        var inactivePolicySuffix = Random.Shared.Next(500, 999);

        // 1. Unknown category -> DomainError.Validation("Finance.Policy.InvalidCategory", ...) -> 422 UnprocessableEntity
        var unknownRequest = new CreateFinancialPolicyRequest(
            PolicyCode: $"FP-2026-{unknownPolicySuffix}",
            Name: "Invalid Category Policy",
            Category: "unknown_category_code",
            BusinessPurpose: "Verify master category validation rejects unknown category codes.",
            ApplicableContext: "All",
            EffectiveDate: today,
            EndDate: today.AddYears(1),
            SupersedesPolicyId: null,
            Notes: null);

        var unknownResponse = await Client.PostAsJsonAsync("/api/finance/policies", unknownRequest);
        unknownResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var unknownProblem = await unknownResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(unknownProblem);
        unknownProblem.Extensions.Should().ContainKey("errorCode");
        unknownProblem.Extensions["errorCode"]?.ToString().Should().Be("Finance.Policy.InvalidCategory");

        // 2. Inactive category -> DomainError.Validation("Finance.Policy.InvalidCategory", ...) -> 422 UnprocessableEntity
        var inactiveCode = $"inactive_cat_{Guid.NewGuid():N}"[..32];
        await ExecuteAsync("""
            INSERT INTO finance.policy_categories (code, name, description, display_order, is_active, created_by)
            VALUES (@Code, N'Inactive Test Category', N'Temporary inactive category for integration test', 99, 0, N'TEST_SETUP');
            """,
            new { Code = inactiveCode });

        try
        {
            var inactiveRequest = new CreateFinancialPolicyRequest(
                PolicyCode: $"FP-2026-{inactivePolicySuffix}",
                Name: "Inactive Category Policy",
                Category: inactiveCode,
                BusinessPurpose: "Verify master category validation rejects inactive category codes.",
                ApplicableContext: "All",
                EffectiveDate: today,
                EndDate: today.AddYears(1),
                SupersedesPolicyId: null,
                Notes: null);

            var inactiveResponse = await Client.PostAsJsonAsync("/api/finance/policies", inactiveRequest);
            inactiveResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var inactiveProblem = await inactiveResponse.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(inactiveProblem);
            inactiveProblem.Extensions.Should().ContainKey("errorCode");
            inactiveProblem.Extensions["errorCode"]?.ToString().Should().Be("Finance.Policy.InvalidCategory");
        }
        finally
        {
            await ExecuteAsync("DELETE FROM finance.policy_categories WHERE code = @Code;", new { Code = inactiveCode });
        }
    }

    [SkippableFact]
    public async Task GetFinancialPolicies_FilteredByGeneralFee_Should_ReturnMatchingPolicies()
    {
        SkipIfDatabaseUnavailable();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var generalPolicyCode = $"FP-2026-{Random.Shared.Next(100, 499)}";
        var tuitionPolicyCode = $"FP-2026-{Random.Shared.Next(500, 999)}";

        var createGeneralResponse = await Client.PostAsJsonAsync("/api/finance/policies", new CreateFinancialPolicyRequest(
            PolicyCode: generalPolicyCode,
            Name: "Hagios Institutional Fee Policy 2026/2027",
            Category: "general_fee",
            BusinessPurpose: "Verify general_fee policies are discoverable via category filter (IS017).",
            ApplicableContext: "All",
            EffectiveDate: today,
            EndDate: today.AddYears(1),
            SupersedesPolicyId: null,
            Notes: null));
        createGeneralResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var generalPolicy = await createGeneralResponse.Content.ReadFromJsonAsync<CreateFinancialPolicyResponse>();
        Assert.NotNull(generalPolicy);

        var createTuitionResponse = await Client.PostAsJsonAsync("/api/finance/policies", new CreateFinancialPolicyRequest(
            PolicyCode: tuitionPolicyCode,
            Name: "Tuition Policy 2026/2027",
            Category: "tuition",
            BusinessPurpose: "Control policy with a different category.",
            ApplicableContext: "All",
            EffectiveDate: today,
            EndDate: today.AddYears(1),
            SupersedesPolicyId: null,
            Notes: null));
        createTuitionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var tuitionPolicy = await createTuitionResponse.Content.ReadFromJsonAsync<CreateFinancialPolicyResponse>();
        Assert.NotNull(tuitionPolicy);

        var filterResponse = await Client.GetAsync("/api/finance/policies?category=general_fee");
        filterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var filtered = await filterResponse.Content.ReadFromJsonAsync<List<FinancialPolicyListItemDto>>();
        Assert.NotNull(filtered);
        filtered.Should().ContainSingle(p => p.Id == generalPolicy.Id && p.Category == "general_fee");
        filtered.Should().NotContain(p => p.Id == tuitionPolicy.Id);
    }
}
