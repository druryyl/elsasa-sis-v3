import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  cancelUnavailabilityWindow,
  createUnavailabilityWindow,
  declareTeacherAvailability,
  getTeacherAvailability,
  getTeacherUnavailabilityWindows,
} from '../api/availabilityApi';
import { availabilityKeys } from '../api/queryKeys';
import type {
  CancelUnavailabilityWindowResponse,
  CreateUnavailabilityWindowRequest,
  CreateUnavailabilityWindowResponse,
  DeclareTeacherAvailabilityRequest,
  DeclareTeacherAvailabilityResponse,
  GetTeacherUnavailabilityWindowsParams,
  TeacherAvailabilityDto,
  TeacherUnavailabilityWindowDto,
} from '../api/types';
import { notifyError, notifySuccess, tryGetToast, type ToastOptions } from './toastNotifications';

export { availabilityKeys };

export interface AvailabilityQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface AvailabilityMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Reactive TanStack Query hook fetching a teacher's declared availability for an academic period (P7-S23, SCR-TCH-04).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useTeacherAvailability(
  teacherId: MaybeRefOrGetter<string | null | undefined>,
  periodId?: MaybeRefOrGetter<string | null | undefined>,
  options?: AvailabilityQueryOptions,
) {
  const resolvedTeacherId = computed(() => {
    const val = toValue(teacherId);
    return val && val.trim() !== '' ? val.trim() : null;
  });

  const resolvedPeriodId = computed(() => {
    const val = toValue(periodId);
    return val && val.trim() !== '' ? val.trim() : null;
  });

  return useQuery<TeacherAvailabilityDto>({
    queryKey: computed(() =>
      availabilityKeys.teacherAvailability(
        resolvedTeacherId.value,
        resolvedPeriodId.value,
      ),
    ),
    queryFn: () => {
      const id = resolvedTeacherId.value;
      if (!id) throw new Error('Teacher ID is required to fetch availability.');
      return getTeacherAvailability(id, resolvedPeriodId.value ?? undefined);
    },
    enabled: computed(() => {
      const isParamEnabled =
        options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isParamEnabled && Boolean(resolvedTeacherId.value);
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching temporary leave and blackout windows for a teacher (P7-S23, SCR-TCH-04).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useUnavailabilityWindows(
  teacherId: MaybeRefOrGetter<string | null | undefined>,
  params?: MaybeRefOrGetter<GetTeacherUnavailabilityWindowsParams | undefined>,
  options?: AvailabilityQueryOptions,
) {
  const resolvedTeacherId = computed(() => {
    const val = toValue(teacherId);
    return val && val.trim() !== '' ? val.trim() : null;
  });

  const resolvedParams = computed(() => toValue(params));

  return useQuery<TeacherUnavailabilityWindowDto[]>({
    queryKey: computed(() =>
      availabilityKeys.windows(resolvedTeacherId.value, resolvedParams.value),
    ),
    queryFn: () => {
      const id = resolvedTeacherId.value;
      if (!id) throw new Error('Teacher ID is required to fetch unavailability windows.');
      return getTeacherUnavailabilityWindows(id, resolvedParams.value);
    },
    enabled: computed(() => {
      const isParamEnabled =
        options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isParamEnabled && Boolean(resolvedTeacherId.value);
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutation Hooks
// ============================================================================

export interface DeclareAvailabilityVariables {
  teacherId: string;
  payload: DeclareTeacherAvailabilityRequest;
}

/**
 * Reactive TanStack Query mutation hook for declaring or updating recurring weekly teacher availability (P7-S23, SCR-TCH-04).
 * Invalidates availability queries and triggers PrimeVue feedback toast.
 */
export function useDeclareAvailabilityMutation(
  options?: AvailabilityMutationOptions<
    DeclareTeacherAvailabilityResponse,
    DeclareAvailabilityVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    DeclareTeacherAvailabilityResponse,
    unknown,
    DeclareAvailabilityVariables
  >({
    mutationFn: ({ teacherId, payload }) =>
      declareTeacherAvailability(teacherId, payload),
    onSuccess: (data, variables) => {
      // Invalidate teacher availability cache
      queryClient.invalidateQueries({
        queryKey: availabilityKeys.teacher(variables.teacherId),
      });
      // Invalidate workload commitments / monitoring queries as capacity may have changed
      queryClient.invalidateQueries({
        queryKey: ['teaching', 'workload'],
      });

      notifySuccess(
        toast,
        'Availability Declared',
        `Successfully saved recurring availability schedule (${data.maxAvailableHours} hrs/week).`,
        options,
      );

      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(
        toast,
        error,
        'Failed to Declare Availability',
        options,
      );

      options?.onError?.(error, variables);
    },
  });
}

export interface CreateUnavailabilityVariables {
  teacherId: string;
  payload: CreateUnavailabilityWindowRequest;
}

/**
 * Reactive TanStack Query mutation hook for creating a temporary leave or blackout window (P7-S23, SCR-TCH-04).
 * Invalidates availability and windows queries and triggers PrimeVue feedback toast.
 */
export function useCreateUnavailabilityMutation(
  options?: AvailabilityMutationOptions<
    CreateUnavailabilityWindowResponse,
    CreateUnavailabilityVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    CreateUnavailabilityWindowResponse,
    unknown,
    CreateUnavailabilityVariables
  >({
    mutationFn: ({ teacherId, payload }) =>
      createUnavailabilityWindow(teacherId, payload),
    onSuccess: (data, variables) => {
      // Invalidate windows and teacher availability cache
      queryClient.invalidateQueries({
        queryKey: availabilityKeys.teacher(variables.teacherId),
      });
      // Invalidate workload commitments / monitoring queries
      queryClient.invalidateQueries({
        queryKey: ['teaching', 'workload'],
      });

      const assignmentNotice =
        data.affectedActiveAssignmentCount > 0
          ? ` Warning: ${data.affectedActiveAssignmentCount} active assignment(s) overlap with this window.`
          : '';

      notifySuccess(
        toast,
        'Leave Window Recorded',
        `Successfully recorded ${data.unavailabilityType.replace(/_/g, ' ')} from ${data.startDate} to ${data.endDate}.${assignmentNotice}`,
        options,
      );

      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(
        toast,
        error,
        'Failed to Record Leave Window',
        options,
      );

      options?.onError?.(error, variables);
    },
  });
}

export interface CancelUnavailabilityVariables {
  windowId: string;
  teacherId?: string;
}

/**
 * Reactive TanStack Query mutation hook for cancelling a temporary leave or blackout window (P7-S23, SCR-TCH-04).
 * Invalidates availability queries and triggers PrimeVue feedback toast.
 */
export function useCancelUnavailabilityMutation(
  options?: AvailabilityMutationOptions<
    CancelUnavailabilityWindowResponse,
    CancelUnavailabilityVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation<
    CancelUnavailabilityWindowResponse,
    unknown,
    CancelUnavailabilityVariables
  >({
    mutationFn: ({ windowId }) => cancelUnavailabilityWindow(windowId),
    onSuccess: (data, variables) => {
      // Invalidate specific teacher queries if provided, or all availability queries
      if (variables.teacherId) {
        queryClient.invalidateQueries({
          queryKey: availabilityKeys.teacher(variables.teacherId),
        });
      } else {
        queryClient.invalidateQueries({
          queryKey: availabilityKeys.all,
        });
      }

      // Invalidate workload commitments / monitoring queries
      queryClient.invalidateQueries({
        queryKey: ['teaching', 'workload'],
      });

      notifySuccess(
        toast,
        'Window Cancelled',
        'The temporary leave/blackout window was successfully cancelled.',
        options,
      );

      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(
        toast,
        error,
        'Failed to Cancel Window',
        options,
      );

      options?.onError?.(error, variables);
    },
  });
}
