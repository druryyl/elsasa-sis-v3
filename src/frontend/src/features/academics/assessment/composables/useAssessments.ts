import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { assessmentsApi } from '../api/assessmentsApi';
import type {
  Assessment,
  CancelAssessmentResponse,
  ConductAssessmentRequest,
  ConductAssessmentResponse,
  CreateAssessmentRequest,
  CreateAssessmentResponse,
  GetAssessmentsFilter,
  UpdateAssessmentRequest,
  UpdateAssessmentResponse,
} from '../types';
import { assessmentKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Types
// ============================================================================

export interface AssessmentQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface AssessmentMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface UpdateAssessmentVariables {
  id: string;
  request: UpdateAssessmentRequest;
}

export interface ConductAssessmentVariables {
  id: string;
  request?: ConductAssessmentRequest;
}

export interface CancelAssessmentVariables {
  id: string;
  reason: string;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching assessments roster under a learning unit (UC-ACD-05, P8-S20).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useAssessmentsQuery(
  unitId: MaybeRefOrGetter<string | null | undefined>,
  filter?: MaybeRefOrGetter<GetAssessmentsFilter | undefined>,
  options?: AssessmentQueryOptions,
) {
  return useQuery<Assessment[]>({
    queryKey: computed(() => {
      const resolvedUnitId = toValue(unitId);
      const resolvedFilter = toValue(filter);
      return assessmentKeys.list(resolvedUnitId, resolvedFilter);
    }),
    queryFn: () => {
      const resolvedUnitId = toValue(unitId);
      if (!resolvedUnitId) {
        throw new Error('Learning Unit ID is required to fetch assessments.');
      }
      return assessmentsApi.listByUnit(resolvedUnitId, toValue(filter));
    },
    enabled: computed(() => {
      const resolvedUnitId = toValue(unitId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedUnitId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching assessment details by ID.
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useAssessmentDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: AssessmentQueryOptions,
) {
  return useQuery<Assessment>({
    queryKey: computed(() => {
      const resolvedId = toValue(id);
      return assessmentKeys.detail(resolvedId);
    }),
    queryFn: () => {
      const resolvedId = toValue(id);
      if (!resolvedId) {
        throw new Error('Assessment ID is required to fetch details.');
      }
      return assessmentsApi.getById(resolvedId);
    },
    enabled: computed(() => {
      const resolvedId = toValue(id);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutations
// ============================================================================

/**
 * Mutation hook for planning and creating a new assessment event (UC-ACD-05, P2-S04).
 */
export function useCreateAssessmentMutation(
  options?: AssessmentMutationOptions<CreateAssessmentResponse, CreateAssessmentRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: CreateAssessmentRequest) => assessmentsApi.create(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assessmentKeys.all });
      notifySuccess(
        toast,
        'Assessment Created',
        `Assessment '${data.title}' planned successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Assessment Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for updating an existing planned assessment (UC-ACD-05, P2-S04).
 */
export function useUpdateAssessmentMutation(
  options?: AssessmentMutationOptions<UpdateAssessmentResponse, UpdateAssessmentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: UpdateAssessmentVariables) =>
      assessmentsApi.update(id, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assessmentKeys.all });
      queryClient.invalidateQueries({ queryKey: assessmentKeys.detail(variables.id) });
      notifySuccess(
        toast,
        'Assessment Updated',
        `Assessment '${data.title}' updated successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Assessment Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for marking an assessment as conducted (UC-ACD-05, P2-S05).
 */
export function useConductAssessmentMutation(
  options?: AssessmentMutationOptions<ConductAssessmentResponse, ConductAssessmentVariables | string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ConductAssessmentVariables | string) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      const request = typeof variables === 'string' ? undefined : variables.request;
      return assessmentsApi.conduct(id, request);
    },
    onSuccess: (data, variables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      queryClient.invalidateQueries({ queryKey: assessmentKeys.all });
      queryClient.invalidateQueries({ queryKey: assessmentKeys.detail(id) });
      notifySuccess(
        toast,
        'Assessment Conducted',
        'Assessment marked as conducted successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Conduct Assessment Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for cancelling a planned assessment with mandatory rationale (UC-ACD-05, P2-S05).
 */
export function useCancelAssessmentMutation(
  options?: AssessmentMutationOptions<CancelAssessmentResponse, CancelAssessmentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, reason }: CancelAssessmentVariables) =>
      assessmentsApi.cancel(id, reason),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assessmentKeys.all });
      queryClient.invalidateQueries({ queryKey: assessmentKeys.detail(variables.id) });
      notifySuccess(
        toast,
        'Assessment Cancelled',
        'Assessment cancelled successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Cancel Assessment Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ============================================================================
// Consolidated Composable
// ============================================================================

/**
 * Primary composable for managing assessment state and operations under a learning unit (P8-S20).
 * Bundles assessment roster query with create, update, conduct, and cancel mutations.
 */
export function useAssessments(
  unitId?: MaybeRefOrGetter<string | null | undefined>,
  filter?: MaybeRefOrGetter<GetAssessmentsFilter | undefined>,
  options?: AssessmentQueryOptions,
) {
  const assessmentsQuery = useAssessmentsQuery(unitId, filter, options);
  const createMutation = useCreateAssessmentMutation();
  const updateMutation = useUpdateAssessmentMutation();
  const conductMutation = useConductAssessmentMutation();
  const cancelMutation = useCancelAssessmentMutation();

  return {
    assessmentsQuery,
    assessments: computed(() => assessmentsQuery.data.value ?? []),
    isLoading: assessmentsQuery.isLoading,
    isPending: assessmentsQuery.isPending,
    isError: assessmentsQuery.isError,
    error: assessmentsQuery.error,
    refetch: assessmentsQuery.refetch,

    createAssessment: createMutation,
    updateAssessment: updateMutation,
    conductAssessment: conductMutation,
    cancelAssessment: cancelMutation,
  };
}
