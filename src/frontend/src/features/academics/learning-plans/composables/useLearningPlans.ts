import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import { learningPlansApi } from '../api/learningPlansApi';
import type {
  ActiveStudentLearningPlan,
  AddAdjustmentRequest,
  AddUnitRequest,
  ApprovePlanRequest,
  ArchivePlanRequest,
  CreatePlanRequest,
  GetLearningPlansParams,
  RevisePlanRequest,
  SlpApproval,
  StudentLearningPlan,
  StudentLearningPlanListItem,
  SyncParticipationsResponse,
  UpdatePlanRequest,
} from '../types';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Query Key Constants & Builders
// ============================================================================

export const learningPlanKeys = {
  all: ['learning-plans'] as const,
  lists: () => ['learning-plans'] as const,
  list: (filters?: GetLearningPlansParams) =>
    ['learning-plans', filters] as const,
  details: () => ['learning-plan'] as const,
  detail: (planId?: string | null | undefined) =>
    ['learning-plan', planId] as const,
  activePlans: () => ['active-learning-plan'] as const,
  activePlan: (
    studentId?: string | null | undefined,
    periodId?: string | null | undefined,
  ) => ['active-learning-plan', studentId, periodId] as const,
} as const;

// ============================================================================
// Option Types
// ============================================================================

export interface LearningPlanQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface LearningPlanMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

// ============================================================================
// Mutation Variable Types & Resolvers
// ============================================================================

export type UpdatePlanVariables =
  | { id: string; request: UpdatePlanRequest }
  | ({ id: string } & UpdatePlanRequest);

function resolveUpdatePlan(variables: UpdatePlanVariables): {
  id: string;
  request: UpdatePlanRequest;
} {
  if ('request' in variables && variables.request) {
    return { id: variables.id, request: variables.request };
  }
  const { id, ...request } = variables as { id: string } & UpdatePlanRequest;
  return { id, request };
}

export type AddUnitVariables =
  | { planId: string; request: AddUnitRequest }
  | ({ planId: string } & AddUnitRequest);

function resolveAddUnit(variables: AddUnitVariables): {
  planId: string;
  request: AddUnitRequest;
} {
  if ('request' in variables && variables.request) {
    return { planId: variables.planId, request: variables.request };
  }
  const { planId, ...request } = variables as { planId: string } & AddUnitRequest;
  return { planId, request };
}

export type RemoveUnitVariables = {
  planId: string;
  unitId: string;
};

export type AddAdjustmentVariables =
  | { planId: string; request: AddAdjustmentRequest }
  | ({ planId: string } & AddAdjustmentRequest);

function resolveAddAdjustment(variables: AddAdjustmentVariables): {
  planId: string;
  request: AddAdjustmentRequest;
} {
  if ('request' in variables && variables.request) {
    return { planId: variables.planId, request: variables.request };
  }
  const { planId, ...request } = variables as { planId: string } & AddAdjustmentRequest;
  return { planId, request };
}

export type RemoveAdjustmentVariables = {
  planId: string;
  adjustmentId: string;
};

export type SubmitPlanVariables =
  | string
  | { id: string };

function resolveSubmitPlan(variables: SubmitPlanVariables): string {
  return typeof variables === 'string' ? variables : variables.id;
}

export type ApprovePlanVariables =
  | { id: string; request: ApprovePlanRequest }
  | ({ id: string } & ApprovePlanRequest);

function resolveApprovePlan(variables: ApprovePlanVariables): {
  id: string;
  request: ApprovePlanRequest;
} {
  if ('request' in variables && variables.request) {
    return { id: variables.id, request: variables.request };
  }
  const { id, ...request } = variables as { id: string } & ApprovePlanRequest;
  return { id, request };
}

export type RevisePlanVariables =
  | { id: string; request: RevisePlanRequest }
  | ({ id: string } & RevisePlanRequest);

function resolveRevisePlan(variables: RevisePlanVariables): {
  id: string;
  request: RevisePlanRequest;
} {
  if ('request' in variables && variables.request) {
    return { id: variables.id, request: variables.request };
  }
  const { id, ...request } = variables as { id: string } & RevisePlanRequest;
  return { id, request };
}

export type ArchivePlanVariables =
  | string
  | { id: string; request?: ArchivePlanRequest };

function resolveArchivePlan(variables: ArchivePlanVariables): {
  id: string;
  request?: ArchivePlanRequest;
} {
  if (typeof variables === 'string') {
    return { id: variables, request: undefined };
  }
  return { id: variables.id, request: variables.request };
}

export type SyncParticipationsVariables =
  | string
  | { id: string };

function resolveSyncParticipations(variables: SyncParticipationsVariables): string {
  return typeof variables === 'string' ? variables : variables.id;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Query composable for paginated, filterable roster with cache key ['learning-plans', filters].
 * Powers the master navigation sidebar in SCR-ACD-04.
 */
export function useLearningPlans(
  params?: MaybeRefOrGetter<GetLearningPlansParams | undefined>,
  options?: LearningPlanQueryOptions,
) {
  return useQuery<PaginatedResponse<StudentLearningPlanListItem>>({
    queryKey: computed(() => {
      const resolvedParams = toValue(params);
      return learningPlanKeys.list(resolvedParams);
    }),
    queryFn: () => {
      const resolvedParams = toValue(params);
      return learningPlansApi.getLearningPlans(resolvedParams);
    },
    enabled: computed(() => {
      return options?.enabled !== undefined ? Boolean(toValue(options.enabled)) : true;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Query composable for single plan inspection with cache key ['learning-plan', planId].
 * Loads constituent units, academic adjustments, and approval history for SCR-ACD-04 detail tabs.
 */
export function useLearningPlanDetail(
  planId: MaybeRefOrGetter<string | null | undefined>,
  options?: LearningPlanQueryOptions,
) {
  return useQuery<StudentLearningPlan>({
    queryKey: computed(() => {
      const resolvedId = toValue(planId);
      return learningPlanKeys.detail(resolvedId);
    }),
    queryFn: () => {
      const resolvedId = toValue(planId);
      if (!resolvedId) {
        throw new Error('Learning Plan ID is required to fetch details.');
      }
      return learningPlansApi.getLearningPlanById(resolvedId);
    },
    enabled: computed(() => {
      const resolvedId = toValue(planId);
      const isEnabled = options?.enabled !== undefined ? Boolean(toValue(options.enabled)) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Query composable for student active plan with cache key ['active-learning-plan', studentId, periodId].
 * Returns null if no active plan exists. Used for contextual badges in SCR-ACD-05 and SCR-ACD-07.
 */
export function useActiveLearningPlan(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  periodId: MaybeRefOrGetter<string | null | undefined>,
  options?: LearningPlanQueryOptions,
) {
  return useQuery<ActiveStudentLearningPlan | null>({
    queryKey: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const resolvedPeriodId = toValue(periodId);
      return learningPlanKeys.activePlan(resolvedStudentId, resolvedPeriodId);
    }),
    queryFn: () => {
      const resolvedStudentId = toValue(studentId);
      const resolvedPeriodId = toValue(periodId);
      if (!resolvedStudentId || !resolvedPeriodId) {
        throw new Error('Student ID and Academic Period ID are required to fetch active plan.');
      }
      return learningPlansApi.getActiveLearningPlan(resolvedStudentId, resolvedPeriodId);
    },
    enabled: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const resolvedPeriodId = toValue(periodId);
      const isEnabled = options?.enabled !== undefined ? Boolean(toValue(options.enabled)) : true;
      return Boolean(resolvedStudentId) && Boolean(resolvedPeriodId) && isEnabled;
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
 * Mutation composable formulating a new initial draft Student Learning Plan (v1).
 * Invalidates ['learning-plans'] and ['active-learning-plan'] upon success.
 */
export function useCreatePlanMutation(
  options?: LearningPlanMutationOptions<{ id: string }, CreatePlanRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: CreatePlanRequest) => learningPlansApi.createPlan(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.all });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.activePlans() });
      notifySuccess(
        toast,
        'Learning Plan Created',
        'Initial draft learning plan created successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Create Learning Plan', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable modifying notes and metadata on a draft plan.
 * Invalidates ['learning-plan', planId] and ['learning-plans'] upon success.
 */
export function useUpdatePlanMutation(
  options?: LearningPlanMutationOptions<void, UpdatePlanVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: UpdatePlanVariables) => {
      const { id, request } = resolveUpdatePlan(variables);
      return learningPlansApi.updatePlan(id, request);
    },
    onSuccess: (data, variables) => {
      const { id } = resolveUpdatePlan(variables);
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.all });
      notifySuccess(
        toast,
        'Learning Plan Updated',
        'Plan notes and metadata updated successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Update Learning Plan', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable linking a constituent learning unit to a draft plan.
 * Invalidates ['learning-plan', planId] upon success.
 */
export function useAddUnitMutation(
  options?: LearningPlanMutationOptions<{ id: string }, AddUnitVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: AddUnitVariables) => {
      const { planId, request } = resolveAddUnit(variables);
      return learningPlansApi.addUnit(planId, request);
    },
    onSuccess: (data, variables) => {
      const { planId } = resolveAddUnit(variables);
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(planId) });
      notifySuccess(
        toast,
        'Learning Unit Added',
        'Constituent learning unit added to plan successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Add Learning Unit', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable removing a constituent learning unit from a draft plan.
 * Invalidates ['learning-plan', planId] upon success.
 */
export function useRemoveUnitMutation(
  options?: LearningPlanMutationOptions<void, RemoveUnitVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ planId, unitId }: RemoveUnitVariables) =>
      learningPlansApi.removeUnit(planId, unitId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(variables.planId) });
      notifySuccess(
        toast,
        'Learning Unit Removed',
        'Constituent learning unit removed from plan.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Remove Learning Unit', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable recording an academic plan adjustment on a draft plan.
 * Invalidates ['learning-plan', planId] upon success.
 */
export function useAddAdjustmentMutation(
  options?: LearningPlanMutationOptions<{ id: string }, AddAdjustmentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: AddAdjustmentVariables) => {
      const { planId, request } = resolveAddAdjustment(variables);
      return learningPlansApi.addAdjustment(planId, request);
    },
    onSuccess: (data, variables) => {
      const { planId } = resolveAddAdjustment(variables);
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(planId) });
      notifySuccess(
        toast,
        'Academic Adjustment Added',
        'Academic plan adjustment added successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Add Academic Adjustment', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable removing an academic plan adjustment from a draft plan.
 * Invalidates ['learning-plan', planId] upon success.
 */
export function useRemoveAdjustmentMutation(
  options?: LearningPlanMutationOptions<void, RemoveAdjustmentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ planId, adjustmentId }: RemoveAdjustmentVariables) =>
      learningPlansApi.removeAdjustment(planId, adjustmentId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(variables.planId) });
      notifySuccess(
        toast,
        'Academic Adjustment Removed',
        'Academic plan adjustment removed from plan.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Remove Academic Adjustment', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable submitting a draft plan for Academic Authority review.
 * Invalidates ['learning-plan', planId] and ['learning-plans'] upon success.
 */
export function useSubmitPlanMutation(
  options?: LearningPlanMutationOptions<void, SubmitPlanVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: SubmitPlanVariables) => {
      const id = resolveSubmitPlan(variables);
      return learningPlansApi.submitPlan(id);
    },
    onSuccess: (data, variables) => {
      const id = resolveSubmitPlan(variables);
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.all });
      notifySuccess(
        toast,
        'Learning Plan Submitted',
        'Student learning plan submitted for Academic Authority review.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Submit Learning Plan', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable executing formal Academic Authority approval (initial or revision approval).
 * Invalidates ['learning-plan', planId], ['learning-plans'], and ['active-learning-plan'] upon success.
 */
export function useApprovePlanMutation(
  options?: LearningPlanMutationOptions<SlpApproval, ApprovePlanVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ApprovePlanVariables) => {
      const { id, request } = resolveApprovePlan(variables);
      return learningPlansApi.approvePlan(id, request);
    },
    onSuccess: (data, variables) => {
      const { id } = resolveApprovePlan(variables);
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.all });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.activePlans() });
      notifySuccess(
        toast,
        'Learning Plan Approved',
        'Student learning plan approved and activated successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Approve Learning Plan', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable cloning an active plan into a new draft revision (v+1).
 * Invalidates ['learning-plan', planId], ['learning-plans'], and ['active-learning-plan'] upon success.
 */
export function useRevisePlanMutation(
  options?: LearningPlanMutationOptions<{ id: string }, RevisePlanVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: RevisePlanVariables) => {
      const { id, request } = resolveRevisePlan(variables);
      return learningPlansApi.revisePlan(id, request);
    },
    onSuccess: (data, variables) => {
      const { id } = resolveRevisePlan(variables);
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.all });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.activePlans() });
      notifySuccess(
        toast,
        'Plan Revision Initiated',
        'New draft revision created for learning plan.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Revise Learning Plan', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable synchronizing approved SLP constituent units into academic participations.
 * Invalidates ['learning-plan', planId] and ['academics', 'participations'] upon success.
 */
export function useSyncParticipationsMutation(
  options?: LearningPlanMutationOptions<SyncParticipationsResponse, SyncParticipationsVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: SyncParticipationsVariables) => {
      const id = resolveSyncParticipations(variables);
      return learningPlansApi.syncParticipations(id);
    },
    onSuccess: (data, variables) => {
      const id = resolveSyncParticipations(variables);
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: ['academics', 'participations'] });
      const detailMsg =
        typeof data?.syncedCount === 'number'
          ? `Synchronized ${data.syncedCount} student academic participations.`
          : 'Student participations synchronized from learning plan.';
      notifySuccess(toast, 'Participations Synchronized', detailMsg, options);
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Synchronize Participations', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation composable archiving or retiring a student learning plan.
 * Invalidates ['learning-plan', planId], ['learning-plans'], and ['active-learning-plan'] upon success.
 */
export function useArchivePlanMutation(
  options?: LearningPlanMutationOptions<void, ArchivePlanVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ArchivePlanVariables) => {
      const { id, request } = resolveArchivePlan(variables);
      return learningPlansApi.archivePlan(id, request);
    },
    onSuccess: (data, variables) => {
      const { id } = resolveArchivePlan(variables);
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.all });
      queryClient.invalidateQueries({ queryKey: learningPlanKeys.activePlans() });
      notifySuccess(
        toast,
        'Learning Plan Archived',
        'Student learning plan archived successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Archive Learning Plan', options);
      options?.onError?.(error, variables);
    },
  });
}

// ============================================================================
// Convenience Aliases
// ============================================================================

export const useCreateLearningPlan = useCreatePlanMutation;
export const useUpdateLearningPlan = useUpdatePlanMutation;
export const useAddUnit = useAddUnitMutation;
export const useRemoveUnit = useRemoveUnitMutation;
export const useAddAdjustment = useAddAdjustmentMutation;
export const useRemoveAdjustment = useRemoveAdjustmentMutation;
export const useSubmitPlan = useSubmitPlanMutation;
export const useApprovePlan = useApprovePlanMutation;
export const useRevisePlan = useRevisePlanMutation;
export const useSyncParticipations = useSyncParticipationsMutation;
export const useArchivePlan = useArchivePlanMutation;

// Mutation-object style aliases
export const createPlanMutation = useCreatePlanMutation;
export const updatePlanMutation = useUpdatePlanMutation;
export const addUnitMutation = useAddUnitMutation;
export const removeUnitMutation = useRemoveUnitMutation;
export const addAdjustmentMutation = useAddAdjustmentMutation;
export const removeAdjustmentMutation = useRemoveAdjustmentMutation;
export const submitPlanMutation = useSubmitPlanMutation;
export const approvePlanMutation = useApprovePlanMutation;
export const revisePlanMutation = useRevisePlanMutation;
export const syncParticipationsMutation = useSyncParticipationsMutation;
export const archivePlanMutation = useArchivePlanMutation;

/**
 * Unified feature composable for Student Learning Plans (SCR-ACD-04, UC-ACD-09, UC-ACD-10).
 */
export function useLearningPlanFeature(
  currentPlanId?: MaybeRefOrGetter<string | null | undefined>,
  activeStudentId?: MaybeRefOrGetter<string | null | undefined>,
  academicPeriodId?: MaybeRefOrGetter<string | null | undefined>,
) {
  const detailQuery = useLearningPlanDetail(currentPlanId);
  const activePlanQuery = useActiveLearningPlan(activeStudentId, academicPeriodId);

  const createMutation = useCreatePlanMutation();
  const updateMutation = useUpdatePlanMutation();
  const addUnitMut = useAddUnitMutation();
  const removeUnitMut = useRemoveUnitMutation();
  const addAdjustmentMut = useAddAdjustmentMutation();
  const removeAdjustmentMut = useRemoveAdjustmentMutation();
  const submitMutation = useSubmitPlanMutation();
  const approveMutation = useApprovePlanMutation();
  const reviseMutation = useRevisePlanMutation();
  const syncParticipationsMut = useSyncParticipationsMutation();
  const archiveMutation = useArchivePlanMutation();

  return {
    detailQuery,
    activePlanQuery,
    createPlanMutation: createMutation,
    updatePlanMutation: updateMutation,
    addUnitMutation: addUnitMut,
    removeUnitMutation: removeUnitMut,
    addAdjustmentMutation: addAdjustmentMut,
    removeAdjustmentMutation: removeAdjustmentMut,
    submitPlanMutation: submitMutation,
    approvePlanMutation: approveMutation,
    revisePlanMutation: reviseMutation,
    syncParticipationsMutation: syncParticipationsMut,
    archivePlanMutation: archiveMutation,
  };
}
