// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  financeKeys,
  useFinancePolicyCategoriesQuery,
  useFinancePoliciesQuery,
  useFinancePolicyDetailQuery,
  useCreatePolicyMutation,
  useApprovePolicyMutation,
  useCreateChargeRuleMutation,
  useFinanceObligationsQuery,
  useBatchAssessPreviewMutation,
  useBatchAssessCommitMutation,
  useAdHocAssessMutation,
  useBillingDocumentsQuery,
  useBillingDocumentDetailQuery,
  useGenerateBillMutation,
  useGenerateStatementMutation,
  useStudentAccountQuery,
  useSearchStudentAccountsQuery,
} from '../index';
import * as policiesApi from '../policies/api/policies.api';
import * as obligationsApi from '../obligations/api/obligations.api';
import * as billingApi from '../billing/api/billing.api';
import * as accountsApi from '../accounts/api/accounts.api';

const mockToast = {
  add: vi.fn(),
  removeGroup: vi.fn(),
  removeAllGroups: vi.fn(),
};

vi.mock('primevue/usetoast', () => ({
  useToast: () => mockToast,
}));

function withSetup<T>(composable: () => T) {
  let result!: T;
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  const app = createApp({
    setup() {
      result = composable();
      return () => {};
    },
  });

  app.use(VueQueryPlugin, { queryClient });
  app.mount(document.createElement('div'));

  return { result, queryClient, app };
}

describe('Finance TanStack Query Composables & Keys (P5-S14)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('financeKeys factory', () => {
    it('generates cohesive policy query keys', () => {
      expect(financeKeys.all).toEqual(['finance']);
      expect(financeKeys.policies.all).toEqual(['finance', 'policies']);
      expect(financeKeys.policies.categories).toEqual(['finance', 'policies', 'categories']);
      expect(financeKeys.policies.lists()).toEqual(['finance', 'policies', 'list']);
      expect(financeKeys.policies.list()).toEqual(['finance', 'policies', 'list']);
      expect(financeKeys.policies.list({ status: 'active' })).toEqual([
        'finance',
        'policies',
        'list',
        { status: 'active' },
      ]);
      expect(financeKeys.policies.details()).toEqual(['finance', 'policies', 'detail']);
      expect(financeKeys.policies.detail('pol-123')).toEqual([
        'finance',
        'policies',
        'detail',
        'pol-123',
      ]);
    });

    it('generates cohesive obligations query keys', () => {
      expect(financeKeys.obligations.all).toEqual(['finance', 'obligations']);
      expect(financeKeys.obligations.lists()).toEqual(['finance', 'obligations', 'list']);
      expect(financeKeys.obligations.list()).toEqual(['finance', 'obligations', 'list']);
      expect(financeKeys.obligations.list({ studentId: 'stu-1' })).toEqual([
        'finance',
        'obligations',
        'list',
        { studentId: 'stu-1' },
      ]);
      expect(financeKeys.obligations.detail('ob-99')).toEqual([
        'finance',
        'obligations',
        'detail',
        'ob-99',
      ]);
    });

    it('generates cohesive billing query keys', () => {
      expect(financeKeys.billing.all).toEqual(['finance', 'billing']);
      expect(financeKeys.billing.lists()).toEqual(['finance', 'billing', 'list']);
      expect(financeKeys.billing.list({ documentType: 'bill' })).toEqual([
        'finance',
        'billing',
        'list',
        { documentType: 'bill' },
      ]);
      expect(financeKeys.billing.detail('bill-456')).toEqual([
        'finance',
        'billing',
        'detail',
        'bill-456',
      ]);
    });

    it('generates cohesive account query keys', () => {
      expect(financeKeys.accounts.all).toEqual(['finance', 'accounts']);
      expect(financeKeys.accounts.lists()).toEqual(['finance', 'accounts', 'list']);
      expect(financeKeys.accounts.list({ campus: 'Jakarta' })).toEqual([
        'finance',
        'accounts',
        'list',
        { campus: 'Jakarta' },
      ]);
      expect(financeKeys.accounts.detail('stu-1')).toEqual([
        'finance',
        'accounts',
        'detail',
        'stu-1',
      ]);
      expect(financeKeys.accounts.balances()).toEqual(['finance', 'accounts', 'balance']);
      expect(financeKeys.accounts.balance('stu-1')).toEqual([
        'finance',
        'accounts',
        'balance',
        'stu-1',
      ]);
    });
  });

  describe('Policies Composables', () => {
    it('useFinancePolicyCategoriesQuery fetches active categories using CACHE_TIERS.STATIC', async () => {
      const mockCategories = [
        {
          code: 'tuition',
          name: 'Tuition (SPP)',
          description: 'Regular academic and instructional fees',
          displayOrder: 1,
          isActive: true,
        },
        {
          code: 'general_fee',
          name: 'General Institutional Fee (Biaya Umum)',
          description: 'Institutional and operational charges',
          displayOrder: 5,
          isActive: true,
        },
      ];
      const getCategoriesSpy = vi
        .spyOn(policiesApi, 'getPolicyCategories')
        .mockResolvedValue(mockCategories);

      const { result, queryClient } = withSetup(() => useFinancePolicyCategoriesQuery());

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(getCategoriesSpy).toHaveBeenCalledTimes(1);
      expect(result.data.value).toEqual(mockCategories);

      const cachedQuery = queryClient
        .getQueryCache()
        .find({ queryKey: financeKeys.policies.categories });
      expect(cachedQuery).toBeDefined();
      expect(cachedQuery?.options.staleTime).toBe(CACHE_TIERS.STATIC.staleTime);
      expect(cachedQuery?.options.gcTime).toBe(CACHE_TIERS.STATIC.gcTime);
      expect(cachedQuery?.options.refetchOnWindowFocus).toBe(
        CACHE_TIERS.STATIC.refetchOnWindowFocus,
      );
    });

    it('useFinancePolicyCategoriesQuery respects enabled option when false', () => {
      const getCategoriesSpy = vi.spyOn(policiesApi, 'getPolicyCategories');
      const { result } = withSetup(() =>
        useFinancePolicyCategoriesQuery({ enabled: false }),
      );

      expect(result.fetchStatus.value).toBe('idle');
      expect(getCategoriesSpy).not.toHaveBeenCalled();
    });

    it('useFinancePoliciesQuery calls getPolicies with reactive filters', async () => {
      const mockPolicies = [
        {
          id: 'pol-1',
          policyCode: 'FP-2026-001',
          name: 'Tuition Policy',
          academicYearId: 'AY2026',
          category: 'tuition',
          status: 'active',
          chargeRules: [],
        },
      ];
      const getPoliciesSpy = vi
        .spyOn(policiesApi, 'getPolicies')
        .mockResolvedValue(mockPolicies as any);

      const filterRef = ref({ status: 'active' as const });
      const { result } = withSetup(() => useFinancePoliciesQuery(filterRef));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(getPoliciesSpy).toHaveBeenCalledWith({ status: 'active' });
      expect(result.data.value).toEqual(mockPolicies);
    });

    it('useFinancePolicyDetailQuery disabled when policyId is not provided', () => {
      const getPolicyByIdSpy = vi.spyOn(policiesApi, 'getPolicyById');
      const { result } = withSetup(() => useFinancePolicyDetailQuery(null));

      expect(result.fetchStatus.value).toBe('idle');
      expect(getPolicyByIdSpy).not.toHaveBeenCalled();
    });

    it('useFinancePolicyDetailQuery fetches policy by ID when resolved', async () => {
      const mockPolicy = {
        id: 'pol-1',
        policyCode: 'FP-2026-001',
        name: 'Tuition Policy',
        chargeRules: [],
      };
      vi.spyOn(policiesApi, 'getPolicyById').mockResolvedValue(mockPolicy as any);

      const { result } = withSetup(() => useFinancePolicyDetailQuery('pol-1'));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockPolicy);
    });

    it('useCreatePolicyMutation creates policy, invalidates policies cache, and triggers toast', async () => {
      const mockResponse = {
        id: 'pol-new',
        policyCode: 'FP-2026-002',
        name: 'New Draft Policy',
        academicYearId: 'AY2026',
        category: 'general_fee',
        effectiveStartDate: '2026-01-01',
        effectiveEndDate: '2026-12-31',
        version: '1.0',
        status: 'draft',
        supersedesPolicyId: null,
        description: null,
        approvingAuthority: 'Finance Director',
        approvedAt: null,
        notes: null,
        chargeRulesCount: 0,
        createdAt: '2026-02-01T00:00:00Z',
        createdBy: 'user',
      };
      vi.spyOn(policiesApi, 'createPolicy').mockResolvedValue(mockResponse);

      const onSuccess = vi.fn();
      const { result, queryClient } = withSetup(() =>
        useCreatePolicyMutation({ onSuccess }),
      );
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        policyCode: 'FP-2026-002',
        name: 'New Draft Policy',
        academicYearId: 'AY2026',
        category: 'general_fee' as const,
        effectiveStartDate: '2026-01-01',
        effectiveEndDate: '2026-12-31',
        version: '1.0',
        approvingAuthority: 'Finance Director',
      };

      await result.mutateAsync(payload);

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.policies.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Policy Created',
        }),
      );
      expect(onSuccess).toHaveBeenCalledWith(mockResponse, payload);
    });

    it('useApprovePolicyMutation approves policy and invalidates policies cache', async () => {
      const mockResponse = {
        id: 'pol-1',
        policyCode: 'FP-2026-001',
        name: 'Tuition Policy',
        status: 'active',
        approvedAt: '2026-02-01T00:00:00Z',
      };
      vi.spyOn(policiesApi, 'approvePolicy').mockResolvedValue(mockResponse as any);

      const { result, queryClient } = withSetup(() => useApprovePolicyMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({ id: 'pol-1', request: { notes: 'Approved for 2026' } });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.policies.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Policy Approved',
        }),
      );
    });

    it('useCreateChargeRuleMutation creates charge rule and invalidates policy detail query', async () => {
      const mockResponse = {
        id: 'cr-new',
        ruleCode: 'CR-2026-NEW',
        policyId: 'pol-1',
        name: 'New Rule',
        chargeType: 'tuition',
        baseAmount: 1000000,
        currencyCode: 'IDR',
        recurrencePattern: 'monthly',
        isMandatory: true,
        priority: 1,
        dueRuleDerivation: 'fixed_day_of_month',
        dueRuleValue: 10,
        targetCampus: null,
        targetGrade: null,
        description: null,
        isActive: true,
        createdAt: '2026-02-01T00:00:00Z',
        createdBy: 'user',
      };
      vi.spyOn(policiesApi, 'createChargeRule').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useCreateChargeRuleMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const variables = {
        policyId: 'pol-1',
        request: {
          ruleCode: 'CR-2026-NEW',
          name: 'New Rule',
          chargeType: 'tuition',
          baseAmount: 1000000,
          recurrencePattern: 'monthly' as const,
          isMandatory: true,
          priority: 1,
          dueRuleDerivation: 'fixed_day_of_month' as const,
          dueRuleValue: 10,
        },
      };

      await result.mutateAsync(variables);

      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: financeKeys.policies.detail('pol-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Charge Rule Created',
        }),
      );
    });
  });

  describe('Obligations Composables', () => {
    it('useFinanceObligationsQuery queries obligations with pagination', async () => {
      const mockResult = {
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      vi.spyOn(obligationsApi, 'getObligations').mockResolvedValue(mockResult);

      const { result } = withSetup(() => useFinanceObligationsQuery({ studentId: 'stu-1' }));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockResult);
    });

    it('useBatchAssessPreviewMutation executes preview without cache invalidation', async () => {
      const mockPreview = {
        totalCandidatesCount: 100,
        eligibleCandidatesCount: 95,
        duplicateCount: 5,
        grossAssessedAmount: 71250000,
        candidates: [],
        createdObligationIds: [],
      };
      vi.spyOn(obligationsApi, 'batchAssessObligations').mockResolvedValue(mockPreview);

      const { result, queryClient } = withSetup(() => useBatchAssessPreviewMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        chargeRuleId: 'cr-1',
        academicPeriodId: 'per-1',
        billingCycleLabel: 'Feb 2026',
      });

      expect(invalidateSpy).not.toHaveBeenCalled();
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Preview Ready',
        }),
      );
    });

    it('useBatchAssessCommitMutation invalidates obligations and accounts caches', async () => {
      const mockCommit = {
        totalCandidatesCount: 100,
        eligibleCandidatesCount: 95,
        duplicateCount: 5,
        grossAssessedAmount: 71250000,
        candidates: [],
        createdObligationIds: ['ob-1', 'ob-2'],
      };
      vi.spyOn(obligationsApi, 'batchAssessObligations').mockResolvedValue(mockCommit);

      const { result, queryClient } = withSetup(() => useBatchAssessCommitMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        chargeRuleId: 'cr-1',
        academicPeriodId: 'per-1',
        billingCycleLabel: 'Feb 2026',
      });

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.obligations.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Batch Assessment Committed',
        }),
      );
    });

    it('useAdHocAssessMutation assesses obligation and invalidates obligations and accounts caches', async () => {
      const mockResponse = {
        id: 'ob-adhoc',
        obligationNumber: 'OBL-2026-ADHOC',
        studentAccountId: 'acc-1',
        studentId: 'stu-1',
        policyId: 'pol-1',
        chargeRuleId: null,
        description: 'Field trip',
        chargeType: 'activity',
        isMandatory: false,
        originalAmount: 150000,
        currencyCode: 'IDR',
        issueDate: '2026-02-01',
        dueDate: '2026-02-15',
        academicPeriodId: 'per-1',
        academicPeriodLabel: 'Semester 1',
        status: 'assessed',
        authorizedBy: 'Teacher',
        notes: null,
        initialScheduleId: 'sch-1',
        createdAt: '2026-02-01T00:00:00Z',
        createdBy: 'user',
      };
      vi.spyOn(obligationsApi, 'assessAdHocObligation').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useAdHocAssessMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        studentId: 'stu-1',
        chargeType: 'activity',
        description: 'Field trip',
        amount: 150000,
        dueDate: '2026-02-15',
        academicPeriodId: 'per-1',
      };

      await result.mutateAsync(payload);

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.obligations.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: financeKeys.accounts.detail('stu-1'),
      });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Obligation Assessed',
        }),
      );
    });
  });

  describe('Billing Composables', () => {
    it('useBillingDocumentsQuery queries billing documents', async () => {
      const mockResult = {
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      vi.spyOn(billingApi, 'getBillingDocuments').mockResolvedValue(mockResult);

      const { result } = withSetup(() => useBillingDocumentsQuery());

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockResult);
    });

    it('useBillingDocumentDetailQuery fetches document by ID', async () => {
      const mockDoc = {
        id: 'bill-1',
        documentNumber: 'INV-2026-0001',
        studentId: 'stu-1',
        totalBilledAmount: 750000,
        items: [],
      };
      vi.spyOn(billingApi, 'getBillingDocumentById').mockResolvedValue(mockDoc as any);

      const { result } = withSetup(() => useBillingDocumentDetailQuery('bill-1'));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockDoc);
    });

    it('useGenerateBillMutation invalidates billing documents, obligations, and accounts', async () => {
      const mockResponse = {
        id: 'bill-1',
        documentNumber: 'INV-2026-0001',
        studentAccountId: 'acc-1',
        studentId: 'stu-1',
        documentType: 'bill',
        totalBilledAmount: 750000,
        currencyCode: 'IDR',
        recipientName: 'Parent',
        recipientRelationship: 'father',
        recipientContact: 'parent@test.com',
        billingPeriodStart: null,
        billingPeriodEnd: null,
        issueDate: '2026-02-01',
        paymentDueDate: '2026-02-15',
        deliveryChannel: 'in_app',
        deliveryStatus: 'pending',
        notes: null,
        itemCount: 1,
        compiledObligationIds: ['ob-1'],
        createdAt: '2026-02-01T00:00:00Z',
        createdBy: 'user',
      };
      vi.spyOn(billingApi, 'generateBill').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useGenerateBillMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        studentId: 'stu-1',
        obligationIds: ['ob-1'],
        dueDate: '2026-02-15',
        notes: 'Monthly invoice',
      };

      await result.mutateAsync(payload);

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.billing.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.obligations.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.accounts.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Bill Generated',
        }),
      );
    });

    it('useGenerateStatementMutation invalidates billing documents cache', async () => {
      const mockResponse = {
        id: 'stmt-1',
        documentNumber: 'STMT-2026-0001',
        studentAccountId: 'acc-1',
        studentId: 'stu-1',
        documentType: 'statement',
        totalBilledAmount: 1500000,
        currencyCode: 'IDR',
        recipientName: 'Parent',
        recipientRelationship: 'mother',
        recipientContact: 'mom@test.com',
        billingPeriodStart: '2026-01-01',
        billingPeriodEnd: '2026-01-31',
        issueDate: '2026-02-01',
        paymentDueDate: null,
        deliveryChannel: 'email',
        deliveryStatus: 'pending',
        notes: null,
        itemCount: 2,
        compiledObligationIds: ['ob-1', 'ob-2'],
        createdAt: '2026-02-01T00:00:00Z',
        createdBy: 'user',
      };
      vi.spyOn(billingApi, 'generateStatement').mockResolvedValue(mockResponse);

      const { result, queryClient } = withSetup(() => useGenerateStatementMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const payload = {
        studentId: 'stu-1',
        startDate: '2026-01-01',
        endDate: '2026-01-31',
      };

      await result.mutateAsync(payload);

      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: financeKeys.billing.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Statement Generated',
        }),
      );
    });
  });

  describe('Accounts Composables', () => {
    it('useStudentAccountQuery fetches 360 account summary with derived balances', async () => {
      const mockSummary = {
        accountId: 'acc-1',
        accountNumber: 'ACC-2026-0001',
        studentId: 'stu-1',
        studentNumber: '2026001',
        studentFullName: 'Ahmad Dahlan',
        currencyCode: 'IDR',
        accountStatus: 'active',
        openedDate: '2026-01-01',
        closedDate: null,
        notes: null,
        createdAt: '2026-01-01T00:00:00Z',
        totalAssessed: 15000000,
        totalSettled: 5000000,
        currentBalance: 10000000,
        openObligations: [],
        recentBillingDocuments: [],
      };
      vi.spyOn(accountsApi, 'getStudentAccount').mockResolvedValue(mockSummary);

      const { result } = withSetup(() => useStudentAccountQuery('stu-1'));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockSummary);
    });

    it('useSearchStudentAccountsQuery searches accounts with filters', async () => {
      const mockResult = {
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      vi.spyOn(accountsApi, 'searchStudentAccounts').mockResolvedValue(mockResult);

      const { result } = withSetup(() =>
        useSearchStudentAccountsQuery({ searchTerm: 'Ahmad' }),
      );

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(result.data.value).toEqual(mockResult);
    });
  });

  describe('Error handling', () => {
    it('displays error toast and calls onError callback on mutation failure', async () => {
      const error = new Error('Network error');
      vi.spyOn(policiesApi, 'createPolicy').mockRejectedValue(error);

      const onError = vi.fn();
      const { result } = withSetup(() => useCreatePolicyMutation({ onError }));

      await expect(
        result.mutateAsync({
          policyCode: 'FP-ERR',
          name: 'Err Policy',
          academicYearId: 'AY',
          category: 'general_fee' as const,
          effectiveStartDate: '2026-01-01',
          effectiveEndDate: '2026-12-31',
          version: '1.0',
          approvingAuthority: 'Finance Director',
        }),
      ).rejects.toThrow('Network error');

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Policy Creation Failed',
        }),
      );
      expect(onError).toHaveBeenCalled();
    });
  });
});
