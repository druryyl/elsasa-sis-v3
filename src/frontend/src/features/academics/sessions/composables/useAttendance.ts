import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  correctAttendance,
  getActivityAttendance,
  getStudentAttendanceSummary,
  recordSessionAttendance,
} from '../api/sessions.api';
import type {
  AttendanceRecord,
  CorrectAttendanceRequest,
  CorrectAttendanceResponse,
  RecordAttendanceRequest,
  RecordAttendanceResponse,
  StudentAttendanceSummary,
} from '../types';
import { learningActivityKeys } from './useLearningActivities';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Query Key Constants & Factories
// ============================================================================

export const attendanceKeys = {
  all: ['academics', 'attendance'] as const,
  activityAttendance: (activityId: string | null | undefined) =>
    ['academics', 'attendance', 'activity', activityId] as const,
  studentSummary: (
    studentId: string | null | undefined,
    academicPeriodId?: string | null | undefined,
  ) =>
    ['academics', 'attendance', 'student-summary', studentId, { academicPeriodId }] as const,
};

// ============================================================================
// Composable Options Types
// ============================================================================

export interface AttendanceQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface AttendanceMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface RecordAttendanceVariables {
  activityId: string;
  request: RecordAttendanceRequest;
}

export interface CorrectAttendanceVariables {
  attendanceRecordId: string;
  activityId?: string;
  studentId?: string;
  request: CorrectAttendanceRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching session roll-call attendance roster and corrections (SCR-ACD-01, P4-S09).
 * Cache tier: TRANSACTIONAL (immediate staleness, 5m gc, refetch on window focus).
 */
export function useActivityAttendanceQuery(
  activityId: MaybeRefOrGetter<string | null | undefined>,
  options?: AttendanceQueryOptions,
) {
  return useQuery<AttendanceRecord[]>({
    queryKey: computed(() => {
      const resolvedId = toValue(activityId);
      return attendanceKeys.activityAttendance(resolvedId);
    }),
    queryFn: () => {
      const resolvedId = toValue(activityId);
      if (!resolvedId) {
        throw new Error('Learning Activity ID is required to fetch attendance.');
      }
      return getActivityAttendance(resolvedId);
    },
    enabled: computed(() => {
      const resolvedId = toValue(activityId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching aggregate attendance summary metrics for a student (P4-S09, TD-04).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useStudentAttendanceSummaryQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  academicPeriodId?: MaybeRefOrGetter<string | null | undefined>,
  options?: AttendanceQueryOptions,
) {
  return useQuery<StudentAttendanceSummary>({
    queryKey: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const resolvedPeriodId = toValue(academicPeriodId);
      return attendanceKeys.studentSummary(resolvedStudentId, resolvedPeriodId);
    }),
    queryFn: () => {
      const resolvedStudentId = toValue(studentId);
      if (!resolvedStudentId) {
        throw new Error('Student ID is required to fetch attendance summary.');
      }
      return getStudentAttendanceSummary(
        resolvedStudentId,
        toValue(academicPeriodId) ?? undefined,
      );
    },
    enabled: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedStudentId) && isEnabled;
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
 * Mutation hook for recording live session roll call within the 24-hour grace window (P4-S08, UC-ACD-03).
 * Invalidates activity attendance, activity detail, and student summary caches upon success.
 */
export function useRecordAttendanceMutation(
  options?: AttendanceMutationOptions<RecordAttendanceResponse, RecordAttendanceVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ activityId, request }: RecordAttendanceVariables) =>
      recordSessionAttendance(activityId, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: attendanceKeys.all });
      queryClient.invalidateQueries({
        queryKey: attendanceKeys.activityAttendance(variables.activityId),
      });
      queryClient.invalidateQueries({
        queryKey: learningActivityKeys.detail(variables.activityId),
      });
      notifySuccess(
        toast,
        'Attendance Recorded',
        `Successfully recorded roll call (${data.recordsUpserted} records saved).`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Attendance Recording Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for submitting an auditable historical attendance correction with mandatory reason (P4-S09, UC-ACD-15).
 * Invalidates attendance rosters and activity summaries upon success.
 */
export function useCorrectAttendanceMutation(
  options?: AttendanceMutationOptions<CorrectAttendanceResponse, CorrectAttendanceVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ attendanceRecordId, request }: CorrectAttendanceVariables) =>
      correctAttendance(attendanceRecordId, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: attendanceKeys.all });
      if (variables.activityId) {
        queryClient.invalidateQueries({
          queryKey: attendanceKeys.activityAttendance(variables.activityId),
        });
        queryClient.invalidateQueries({
          queryKey: learningActivityKeys.detail(variables.activityId),
        });
      }
      if (variables.studentId) {
        queryClient.invalidateQueries({
          queryKey: attendanceKeys.studentSummary(variables.studentId),
        });
      }
      notifySuccess(
        toast,
        'Attendance Corrected',
        `Status updated from '${data.previousStatus}' to '${data.newStatus}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Attendance Correction Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
