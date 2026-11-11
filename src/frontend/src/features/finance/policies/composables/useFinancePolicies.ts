import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getPolicyCategories,
  getPolicies,
  getPolicyById,
  createPolicy,
  approvePolicy,
  createChargeRule,
} from '../api/policies.api';
import type {
  PolicyCategoryDto,
  FinancialPolicyDto,
  FinancialPolicyFilters,
  CreateFinancialPolicyRequest,
  CreateFinancialPolicyResponse,
  ApproveFinancialPolicyRequest,
  ApproveFinancialPolicyResponse,
  CreateChargeRuleRequest,
  CreateChargeRuleResponse,
} from '../types/policies.types';
import { financeKeys } from '../../shared/queryKeys';
import {
  tryGetToast,
  notifySuccess,
  notifyError,
  type FinanceMutationOptions,
  type FinanceQueryOptions,
} from '../../shared/toastNotifications';

/**
 * Reactive TanStack Query hook fetching active financial policy categories for filters and forms.
 * Uses STATIC cache tier (30m stale, 60m gc, no refetch on window focus).
 */
export function useFinancePolicyCategoriesQuery(options?: FinanceQueryOptions) {
  return useQuery<PolicyCategoryDto[]>({
    queryKey: financeKeys.policies.categories,
    queryFn: () => getPolicyCategories(),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.STATIC.staleTime,
    gcTime: CACHE_TIERS.STATIC.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.STATIC.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching financial policies matching optional filters (FIN-WF-01, UC-FIN-10).
 * Uses OPERATIONAL cache tier (5m stale, 15m gc, refetch on window focus).
 */
export function useFinancePoliciesQuery(
  filters?: MaybeRefOrGetter<FinancialPolicyFilters | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<FinancialPolicyDto[]>({
    queryKey: computed(() => financeKeys.policies.list(toValue(filters))),
    queryFn: () => getPolicies(toValue(filters)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching complete policy dossier including charge rules (SCR-FIN-04).
 * Uses OPERATIONAL cache tier.
 */
export function useFinancePolicyDetailQuery(
  policyId: MaybeRefOrGetter<string | null | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<FinancialPolicyDto>({
    queryKey: computed(() => financeKeys.policies.detail(toValue(policyId) || '')),
    queryFn: () => getPolicyById(toValue(policyId)!),
    enabled: computed(() => {
      const resolvedId = toValue(policyId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Mutation hook creating a new financial policy draft (FIN-WF-01, UC-FIN-10, SCR-FIN-04).
 * Automatically invalidates policy query caches upon success.
 */
export function useCreatePolicyMutation(
  options?: FinanceMutationOptions<CreateFinancialPolicyResponse, CreateFinancialPolicyRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: CreateFinancialPolicyRequest) => createPolicy(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.policies.all });
      notifySuccess(
        toast,
        'Policy Created',
        `Financial policy '${data.policyCode}' was created successfully as draft.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Policy Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export type ApprovePolicyVariables =
  | string
  | { id: string; request?: ApproveFinancialPolicyRequest };

/**
 * Mutation hook approving a financial policy draft, transitioning it to active status (Finance Authority).
 * Automatically invalidates policy query caches upon success.
 */
export function useApprovePolicyMutation(
  options?: FinanceMutationOptions<ApproveFinancialPolicyResponse, ApprovePolicyVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ApprovePolicyVariables) => {
      if (typeof variables === 'string') {
        return approvePolicy(variables);
      }
      return approvePolicy(variables.id, variables.request);
    },
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.policies.all });
      notifySuccess(
        toast,
        'Policy Approved',
        `Financial policy '${data.policyCode}' is now active.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Policy Approval Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export interface CreateChargeRuleVariables {
  policyId: string;
  request: CreateChargeRuleRequest;
}

/**
 * Mutation hook creating an operational charge rule anchored to a governing policy (SCR-FIN-04).
 * Automatically invalidates policy detail query caches upon success.
 */
export function useCreateChargeRuleMutation(
  options?: FinanceMutationOptions<CreateChargeRuleResponse, CreateChargeRuleVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ policyId, request }: CreateChargeRuleVariables) =>
      createChargeRule(policyId, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({
        queryKey: financeKeys.policies.detail(variables.policyId),
      });
      notifySuccess(
        toast,
        'Charge Rule Created',
        `Charge rule '${data.ruleCode}' added successfully to policy.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Create Charge Rule Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
