import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { interventionsApi } from '../api/interventionsApi';
import type {
  AcademicIntervention,
  AtRiskLearner,
  GetInterventionsParams,
  InitiateInterventionRequest,
  UpdateInterventionStatusRequest,
} from '../types';
import { interventionKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// Re-export query keys for external consumers
export { interventionKeys };

// ============================================================================
// Types
// ============================================================================

export interface InterventionQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface InterventionMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface UpdateInterventionStatusVariables {
  id: string;
  request: UpdateInterventionStatusRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching dynamic early-warning at-risk learners (UC-ACD-11, P7-S20).
 * Query key: ['academics', 'interventions', 'at-risk', periodId, gradeLevel]
 */
export function useAtRiskLearnersQuery(
  periodId: MaybeRefOrGetter<string | null | undefined>,
  gradeLevel?: MaybeRefOrGetter<string | null | undefined>,
  options?: InterventionQueryOptions,
) {
  return useQuery<AtRiskLearner[]>({
    queryKey: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedGradeLevel = toValue(gradeLevel);
      return interventionKeys.atRisk(resolvedPeriodId, resolvedGradeLevel);
    }),
    queryFn: () => {
      const resolvedPeriodId = toValue(periodId);
      if (!resolvedPeriodId) {
        throw new Error('Academic Period ID is required to fetch at-risk learners.');
      }
      return interventionsApi.getAtRiskLearners({
        academicPeriodId: resolvedPeriodId,
        gradeLevel: toValue(gradeLevel) ?? undefined,
      });
    },
    enabled: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedPeriodId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching active interventions work queue for an academic period (UC-ACD-11, P7-S20).
 * Query key: ['academics', 'interventions', 'active', periodId]
 */
export function useActiveInterventionsQuery(
  periodId: MaybeRefOrGetter<string | null | undefined>,
  options?: InterventionQueryOptions,
) {
  return useQuery<AcademicIntervention[]>({
    queryKey: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      return interventionKeys.active(resolvedPeriodId);
    }),
    queryFn: () => {
      const resolvedPeriodId = toValue(periodId);
      return interventionsApi.getInterventions({
        academicPeriodId: resolvedPeriodId ?? undefined,
      });
    },
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching filtered interventions list.
 */
export function useInterventionsQuery(
  params?: MaybeRefOrGetter<GetInterventionsParams | undefined>,
  options?: InterventionQueryOptions,
) {
  return useQuery<AcademicIntervention[]>({
    queryKey: computed(() => {
      const resolvedParams = toValue(params);
      return interventionKeys.list(resolvedParams);
    }),
    queryFn: () => {
      const resolvedParams = toValue(params);
      return interventionsApi.getInterventions(resolvedParams);
    },
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching detailed intervention plan by ID.
 */
export function useInterventionDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: InterventionQueryOptions,
) {
  return useQuery<AcademicIntervention>({
    queryKey: computed(() => {
      const resolvedId = toValue(id);
      return interventionKeys.detail(resolvedId);
    }),
    queryFn: () => {
      const resolvedId = toValue(id);
      if (!resolvedId) {
        throw new Error('Intervention ID is required to fetch detail.');
      }
      return interventionsApi.getInterventionById(resolvedId);
    },
    enabled: computed(() => {
      const resolvedId = toValue(id);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutations
// ============================================================================

/**
 * Mutation hook for initiating a structured academic intervention support plan (UC-ACD-11, P7-S20).
 * Invalidates intervention caches upon success and shows notification toast.
 */
export function useInitiateInterventionMutation(
  options?: InterventionMutationOptions<AcademicIntervention, InitiateInterventionRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: InitiateInterventionRequest) =>
      interventionsApi.initiateIntervention(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: interventionKeys.all });
      notifySuccess(
        toast,
        'Intervention Initiated',
        `Intervention plan formulated for student.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Initiate Intervention', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for updating intervention status and outcome notes (UC-ACD-11, P7-S20).
 * Invalidates intervention caches upon success and shows notification toast.
 */
export function useUpdateInterventionStatusMutation(
  options?: InterventionMutationOptions<
    AcademicIntervention,
    UpdateInterventionStatusVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: UpdateInterventionStatusVariables) =>
      interventionsApi.updateInterventionStatus(id, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: interventionKeys.all });
      queryClient.invalidateQueries({
        queryKey: interventionKeys.detail(variables.id),
      });
      notifySuccess(
        toast,
        'Intervention Status Updated',
        `Intervention status changed to ${data.status}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Update Intervention Status', options);
      options?.onError?.(error, variables);
    },
  });
}

// Aliases matching criteria naming
export const initiateInterventionMutation = useInitiateInterventionMutation;
export const updateStatusMutation = useUpdateInterventionStatusMutation;

/**
 * Unified feature composable for Academic Interventions (SCR-ACD-06, UC-ACD-11, P7-S20).
 */
export function useAcademicInterventions(
  periodId?: MaybeRefOrGetter<string | null | undefined>,
  gradeLevel?: MaybeRefOrGetter<string | null | undefined>,
) {
  const atRiskQuery = useAtRiskLearnersQuery(periodId, gradeLevel);
  const activeInterventionsQuery = useActiveInterventionsQuery(periodId);
  const initiateMutation = useInitiateInterventionMutation();
  const updateMutation = useUpdateInterventionStatusMutation();

  return {
    atRiskQuery,
    activeInterventionsQuery,
    initiateInterventionMutation: initiateMutation,
    updateStatusMutation: updateMutation,
  };
}
