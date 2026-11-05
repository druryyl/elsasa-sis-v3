import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  cancelLearningActivity,
  conductLearningActivity,
  getActivitiesByUnit,
  getActivityById,
  getTeacherDailyActivities,
  planLearningActivity,
} from '../api/sessions.api';
import type {
  ActivityDetail,
  CancelActivityRequest,
  CancelActivityResponse,
  ConductActivityRequest,
  ConductActivityResponse,
  LearningActivity,
  PlanActivityRequest,
  PlanActivityResponse,
  TeacherDailyActivity,
  UnitActivitiesFilter,
} from '../types';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Query Key Constants & Factories
// ============================================================================

export const learningActivityKeys = {
  all: ['academics', 'activities'] as const,
  unitActivities: (unitId: string | null | undefined, filter?: UnitActivitiesFilter) =>
    filter !== undefined
      ? (['academics', 'activities', 'unit', unitId, filter] as const)
      : (['academics', 'activities', 'unit', unitId] as const),
  teacherDaily: (date?: string | null | undefined) =>
    date !== undefined
      ? (['academics', 'activities', 'teacher', date] as const)
      : (['academics', 'activities', 'teacher'] as const),
  detail: (id: string | null | undefined) =>
    ['academics', 'activities', 'detail', id] as const,
};

// ============================================================================
// Composable Options Types
// ============================================================================

export interface LearningActivityQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface LearningActivityMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface ConductActivityVariables {
  id: string;
  request: ConductActivityRequest;
}

export interface CancelActivityVariables {
  id: string;
  request: CancelActivityRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching instructional activities for a learning unit (P7-S18).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useUnitActivitiesQuery(
  unitId: MaybeRefOrGetter<string | null | undefined>,
  filter?: MaybeRefOrGetter<UnitActivitiesFilter | undefined>,
  options?: LearningActivityQueryOptions,
) {
  return useQuery<LearningActivity[]>({
    queryKey: computed(() => {
      const resolvedUnitId = toValue(unitId);
      const resolvedFilter = toValue(filter);
      return learningActivityKeys.unitActivities(resolvedUnitId, resolvedFilter);
    }),
    queryFn: () => {
      const resolvedUnitId = toValue(unitId);
      if (!resolvedUnitId) {
        throw new Error('Learning Unit ID is required to fetch unit activities.');
      }
      return getActivitiesByUnit(resolvedUnitId, toValue(filter));
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
 * Reactive TanStack Query hook fetching daily instructional activities for the authenticated teacher (SCR-ACD-01, P7-S18).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useTeacherDailyActivitiesQuery(
  date?: MaybeRefOrGetter<string | null | undefined>,
  options?: LearningActivityQueryOptions,
) {
  return useQuery<TeacherDailyActivity[]>({
    queryKey: computed(() => {
      const resolvedDate = toValue(date);
      return learningActivityKeys.teacherDaily(resolvedDate);
    }),
    queryFn: () => {
      const resolvedDate = toValue(date);
      return getTeacherDailyActivities(resolvedDate ?? undefined);
    },
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching activity inspection detail including attendance counts and assignments (P7-S18).
 * Cache tier: TRANSACTIONAL (immediate staleness, 5m gc, refetch on window focus).
 */
export function useActivityDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: LearningActivityQueryOptions,
) {
  return useQuery<ActivityDetail>({
    queryKey: computed(() => {
      const resolvedId = toValue(id);
      return learningActivityKeys.detail(resolvedId);
    }),
    queryFn: () => {
      const resolvedId = toValue(id);
      if (!resolvedId) {
        throw new Error('Learning Activity ID is required to fetch details.');
      }
      return getActivityById(resolvedId);
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
 * Mutation hook for scheduling a new learning activity or homeroom session (P3-S07, TD-03).
 * Invalidates activities cache queries upon success.
 */
export function usePlanActivityMutation(
  options?: LearningActivityMutationOptions<PlanActivityResponse, PlanActivityRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: PlanActivityRequest) => planLearningActivity(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: learningActivityKeys.all });
      notifySuccess(
        toast,
        'Activity Scheduled',
        `Learning activity '${data.title}' scheduled for ${data.activityDate}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Activity Scheduling Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for conducting a session, logging curriculum milestones and delivery notes (UC-ACD-02, P3-S07).
 * Invalidates activities cache queries and detail view upon success.
 */
export function useConductActivityMutation(
  options?: LearningActivityMutationOptions<ConductActivityResponse, ConductActivityVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: ConductActivityVariables) =>
      conductLearningActivity(id, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: learningActivityKeys.all });
      queryClient.invalidateQueries({ queryKey: learningActivityKeys.detail(variables.id) });
      notifySuccess(
        toast,
        'Activity Conducted',
        `Activity completed at ${data.conductedAt}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Activity Conduction Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for cancelling a scheduled learning activity with reason (P3-S07).
 * Invalidates activities cache queries upon success.
 */
export function useCancelActivityMutation(
  options?: LearningActivityMutationOptions<CancelActivityResponse, CancelActivityVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: CancelActivityVariables) =>
      cancelLearningActivity(id, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: learningActivityKeys.all });
      queryClient.invalidateQueries({ queryKey: learningActivityKeys.detail(variables.id) });
      notifySuccess(
        toast,
        'Activity Cancelled',
        `Activity cancelled successfully with status '${data.status}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Activity Cancellation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
