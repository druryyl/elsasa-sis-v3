import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  closeAssignment,
  createAssignment,
  getAssignmentsByActivity,
  getAssignmentsByUnit,
  getAssignmentSubmissions,
  recordSubmissionException,
  releaseAssignment,
} from '../api/coursework.api';
import type {
  Assignment,
  AssignmentSubmission,
  CloseAssignmentResponse,
  CreateAssignmentRequest,
  CreateAssignmentResponse,
  RecordExceptionRequest,
  RecordExceptionResponse,
  ReleaseAssignmentResponse,
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

export const assignmentKeys = {
  all: ['academics', 'assignments'] as const,
  activityAssignments: (activityId: string | null | undefined) =>
    ['academics', 'assignments', 'activity', activityId] as const,
  unitAssignments: (unitId: string | null | undefined) =>
    ['academics', 'assignments', 'unit', unitId] as const,
  submissions: (assignmentId: string | null | undefined) =>
    ['academics', 'assignments', 'submissions', assignmentId] as const,
};

// ============================================================================
// Composable Options Types
// ============================================================================

export interface AssignmentQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface AssignmentMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface CreateAssignmentVariables {
  activityId: string;
  request: CreateAssignmentRequest;
}

export interface RecordExceptionVariables {
  assignmentId: string;
  request: RecordExceptionRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching coursework assignments issued under a learning activity (P5-S10).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useActivityAssignmentsQuery(
  activityId: MaybeRefOrGetter<string | null | undefined>,
  options?: AssignmentQueryOptions,
) {
  return useQuery<Assignment[]>({
    queryKey: computed(() => {
      const resolvedActivityId = toValue(activityId);
      return assignmentKeys.activityAssignments(resolvedActivityId);
    }),
    queryFn: () => {
      const resolvedActivityId = toValue(activityId);
      if (!resolvedActivityId) {
        throw new Error('Activity ID is required to fetch activity assignments.');
      }
      return getAssignmentsByActivity(resolvedActivityId);
    },
    enabled: computed(() => {
      const resolvedActivityId = toValue(activityId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedActivityId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching coursework assignments across a learning unit (P5-S10).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useUnitAssignmentsQuery(
  unitId: MaybeRefOrGetter<string | null | undefined>,
  options?: AssignmentQueryOptions,
) {
  return useQuery<Assignment[]>({
    queryKey: computed(() => {
      const resolvedUnitId = toValue(unitId);
      return assignmentKeys.unitAssignments(resolvedUnitId);
    }),
    queryFn: () => {
      const resolvedUnitId = toValue(unitId);
      if (!resolvedUnitId) {
        throw new Error('Learning Unit ID is required to fetch unit assignments.');
      }
      return getAssignmentsByUnit(resolvedUnitId);
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
 * Reactive TanStack Query hook fetching submission roster for an assignment with on-the-fly missing projection (SCR-ACD-02, TD-06, P5-S12).
 * Cache tier: TRANSACTIONAL (immediate staleness, 5m gc, refetch on window focus).
 */
export function useAssignmentSubmissionsQuery(
  assignmentId: MaybeRefOrGetter<string | null | undefined>,
  options?: AssignmentQueryOptions,
) {
  return useQuery<AssignmentSubmission[]>({
    queryKey: computed(() => {
      const resolvedId = toValue(assignmentId);
      return assignmentKeys.submissions(resolvedId);
    }),
    queryFn: () => {
      const resolvedId = toValue(assignmentId);
      if (!resolvedId) {
        throw new Error('Assignment ID is required to fetch submissions.');
      }
      return getAssignmentSubmissions(resolvedId);
    },
    enabled: computed(() => {
      const resolvedId = toValue(assignmentId);
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
 * Mutation hook for creating a new coursework assignment under a learning activity (P5-S10, UC-ACD-04).
 * Invalidates assignments query cache upon success.
 */
export function useCreateAssignmentMutation(
  options?: AssignmentMutationOptions<CreateAssignmentResponse, CreateAssignmentVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ activityId, request }: CreateAssignmentVariables) =>
      createAssignment(activityId, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assignmentKeys.all });
      queryClient.invalidateQueries({
        queryKey: assignmentKeys.activityAssignments(variables.activityId),
      });
      notifySuccess(
        toast,
        'Assignment Created',
        `Assignment '${data.title}' created in draft status.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Assignment Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for releasing a draft assignment to enrolled students (P5-S10).
 * Invalidates assignments query cache upon success.
 */
export function useReleaseAssignmentMutation(
  options?: AssignmentMutationOptions<ReleaseAssignmentResponse, string | { id: string }>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: string | { id: string }) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      return releaseAssignment(id);
    },
    onSuccess: (data, variables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      queryClient.invalidateQueries({ queryKey: assignmentKeys.all });
      queryClient.invalidateQueries({ queryKey: assignmentKeys.submissions(id) });
      notifySuccess(
        toast,
        'Assignment Released',
        `Assignment released successfully at ${data.releasedAt}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Assignment Release Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for closing an assignment against further standard submissions (P5-S10).
 * Invalidates assignments query cache upon success.
 */
export function useCloseAssignmentMutation(
  options?: AssignmentMutationOptions<CloseAssignmentResponse, string | { id: string }>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: string | { id: string }) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      return closeAssignment(id);
    },
    onSuccess: (data, variables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      queryClient.invalidateQueries({ queryKey: assignmentKeys.all });
      queryClient.invalidateQueries({ queryKey: assignmentKeys.submissions(id) });
      notifySuccess(
        toast,
        'Assignment Closed',
        `Assignment closed successfully at ${data.closedAt}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Assignment Closure Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for recording an administrative policy exception ('excused' or 'exempted') (P5-S11, TD-06).
 * Invalidates assignment submissions roster upon success.
 */
export function useRecordExceptionMutation(
  options?: AssignmentMutationOptions<RecordExceptionResponse, RecordExceptionVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ assignmentId, request }: RecordExceptionVariables) =>
      recordSubmissionException(assignmentId, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: assignmentKeys.all });
      queryClient.invalidateQueries({
        queryKey: assignmentKeys.submissions(variables.assignmentId),
      });
      notifySuccess(
        toast,
        'Exception Recorded',
        `Policy exception '${data.exceptionStatus}' recorded successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Exception Recording Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
