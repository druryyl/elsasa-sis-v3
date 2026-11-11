import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  approveWorkloadException,
  getWorkloadCommitments,
  getWorkloadExceptions,
  getWorkloadPolicies,
  recalculateWorkload,
  requestWorkloadException,
} from '../api/workloadApi';
import { workloadKeys } from '../api/queryKeys';
import type {
  ApproveWorkloadExceptionRequest,
  ApproveWorkloadExceptionResponse,
  GetWorkloadCommitmentsParams,
  GetWorkloadExceptionsParams,
  GetWorkloadPoliciesParams,
  RecalculatePeriodWorkloadRequest,
  RecalculatePeriodWorkloadResponse,
  RequestWorkloadExceptionRequest,
  RequestWorkloadExceptionResponse,
  TeacherWorkloadCommitmentDto,
  WorkloadExceptionDto,
  WorkloadPolicyDto,
} from '../api/types';
import { notifyError, notifySuccess, tryGetToast, type ToastOptions } from './toastNotifications';

export { workloadKeys };

export interface WorkloadQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface WorkloadMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Reactive TanStack Query hook fetching institutional workload policies (P7-S22, P3-S09, SCR-TCH-03).
 * Uses REFERENCE cache tier (1h stale, 24h gc, no refetch on focus).
 */
export function useWorkloadPolicies(
  params?: MaybeRefOrGetter<GetWorkloadPoliciesParams | undefined>,
  options?: WorkloadQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<WorkloadPolicyDto[]>({
    queryKey: computed(() => workloadKeys.policyList(resolvedParams.value)),
    queryFn: () => getWorkloadPolicies(resolvedParams.value),
    enabled: computed(() => {
      return options?.enabled !== undefined ? toValue(options.enabled) : true;
    }),
    staleTime: CACHE_TIERS.STATIC.staleTime,
    gcTime: CACHE_TIERS.STATIC.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.STATIC.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching faculty workload capacity monitoring snapshots (P7-S22, P3-S09, SCR-TCH-03).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useWorkloadCommitments(
  params?: MaybeRefOrGetter<GetWorkloadCommitmentsParams | undefined>,
  options?: WorkloadQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<TeacherWorkloadCommitmentDto[]>({
    queryKey: computed(() => workloadKeys.commitmentList(resolvedParams.value)),
    queryFn: () => getWorkloadCommitments(resolvedParams.value),
    enabled: computed(() => {
      return options?.enabled !== undefined ? toValue(options.enabled) : true;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching formal workload exceptions (P7-S22, P3-S10, SCR-TCH-03).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useWorkloadExceptions(
  params?: MaybeRefOrGetter<GetWorkloadExceptionsParams | undefined>,
  options?: WorkloadQueryOptions,
) {
  const resolvedParams = computed(() => toValue(params));

  return useQuery<WorkloadExceptionDto[]>({
    queryKey: computed(() => workloadKeys.exceptionList(resolvedParams.value)),
    queryFn: () => getWorkloadExceptions(resolvedParams.value),
    enabled: computed(() => {
      return options?.enabled !== undefined ? toValue(options.enabled) : true;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutation Hooks
// ============================================================================

export type RecalculateWorkloadVariables = string | RecalculatePeriodWorkloadRequest;

/**
 * Reactive TanStack Query mutation hook for on-demand period faculty capacity recalculation (P7-S22, P3-S09, SCR-TCH-03).
 * Invalidates commitments, exceptions, and assignment caches and triggers PrimeVue feedback toast.
 */
export function useRecalculateWorkloadMutation(
  options?: WorkloadMutationOptions<
    RecalculatePeriodWorkloadResponse,
    RecalculateWorkloadVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    RecalculatePeriodWorkloadResponse,
    unknown,
    RecalculateWorkloadVariables
  >({
    mutationFn: (variables) => recalculateWorkload(variables),
    onSuccess: (data, variables) => {
      // Invalidate all workload monitoring snapshots
      queryClient.invalidateQueries({
        queryKey: workloadKeys.commitments(),
      });
      // Invalidate general workload queries
      queryClient.invalidateQueries({
        queryKey: workloadKeys.all,
      });
      // Invalidate teaching assignments queries since statuses/conflicts might align
      queryClient.invalidateQueries({
        queryKey: ['teaching', 'assignments'],
      });

      notifySuccess(
        toast,
        'Workload Recalculated',
        data.message || `Successfully reconciled ${data.recalculatedCount} faculty commitment records.`,
        options,
      );

      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(
        toast,
        error,
        'Failed to Recalculate Workload',
        options,
      );

      options?.onError?.(error, variables);
    },
  });
}

export interface RequestExceptionVariables {
  payload: RequestWorkloadExceptionRequest;
}

/**
 * Reactive TanStack Query mutation hook for requesting or recommending a workload exception (P7-S22, P3-S10, SCR-TCH-03).
 * Invalidates exceptions and commitments queries and triggers PrimeVue feedback toast.
 */
export function useRequestExceptionMutation(
  options?: WorkloadMutationOptions<
    RequestWorkloadExceptionResponse,
    RequestExceptionVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    RequestWorkloadExceptionResponse,
    unknown,
    RequestExceptionVariables
  >({
    mutationFn: ({ payload }) => requestWorkloadException(payload),
    onSuccess: (data, variables) => {
      // Invalidate workload exceptions cache
      queryClient.invalidateQueries({
        queryKey: workloadKeys.exceptions(),
      });
      // Invalidate workload commitments cache
      queryClient.invalidateQueries({
        queryKey: workloadKeys.commitments(),
      });
      // Invalidate conflicts cache if present
      queryClient.invalidateQueries({
        queryKey: ['teaching', 'conflicts'],
      });

      notifySuccess(
        toast,
        'Exception Requested',
        `Workload exception ${data.exceptionNumber} (${data.exceededHours} hrs) has been submitted for review.`,
        options,
      );

      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(
        toast,
        error,
        'Failed to Request Exception',
        options,
      );

      options?.onError?.(error, variables);
    },
  });
}

export interface ApproveExceptionVariables {
  id: string;
  payload?: ApproveWorkloadExceptionRequest;
}

/**
 * Reactive TanStack Query mutation hook for approving a formal workload exception (P7-S22, P3-S10, SCR-TCH-03).
 * Enforces institutional governance, invalidates commitments, exceptions, and conflicts caches,
 * and triggers PrimeVue feedback toast.
 */
export function useApproveExceptionMutation(
  options?: WorkloadMutationOptions<
    ApproveWorkloadExceptionResponse,
    ApproveExceptionVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    ApproveWorkloadExceptionResponse,
    unknown,
    ApproveExceptionVariables
  >({
    mutationFn: ({ id, payload }) => approveWorkloadException(id, payload),
    onSuccess: (data, variables) => {
      // Invalidate workload exceptions cache
      queryClient.invalidateQueries({
        queryKey: workloadKeys.exceptions(),
      });
      // Invalidate workload commitments cache
      queryClient.invalidateQueries({
        queryKey: workloadKeys.commitments(),
      });
      // Invalidate conflicts cache as capacity conflicts might be waived
      queryClient.invalidateQueries({
        queryKey: ['teaching', 'conflicts'],
      });
      queryClient.invalidateQueries({
        queryKey: ['teaching', 'assignments'],
      });

      const conflictMsg =
        data.resolvedConflictsCount > 0
          ? ` Resolved ${data.resolvedConflictsCount} capacity conflict(s).`
          : '';

      notifySuccess(
        toast,
        'Exception Approved',
        `Exception ${data.exceptionNumber} approved.${conflictMsg}`,
        options,
      );

      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(
        toast,
        error,
        'Failed to Approve Exception',
        options,
      );

      options?.onError?.(error, variables);
    },
  });
}
