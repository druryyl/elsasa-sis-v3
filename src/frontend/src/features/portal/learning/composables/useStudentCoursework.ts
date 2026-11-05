import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getStudentCourseworkWorkspace,
  submitCoursework,
  uploadSubmissionFile,
} from '../api/learning.api';
import type {
  CourseworkStatusFilter,
  FileUploadResponse,
  StudentCourseworkTask,
  SubmitCourseworkRequest,
  SubmitCourseworkResponse,
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

export const studentCourseworkKeys = {
  all: ['portal', 'coursework'] as const,
  workspace: (statusFilter?: string | null | undefined) =>
    statusFilter !== undefined
      ? (['portal', 'coursework', 'workspace', statusFilter] as const)
      : (['portal', 'coursework', 'workspace'] as const),
};

// ============================================================================
// Composable Options Types
// ============================================================================

export interface StudentCourseworkQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface StudentCourseworkMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface SubmitCourseworkVariables {
  assignmentId: string;
  request: SubmitCourseworkRequest;
}

export interface UploadSubmissionFileVariables {
  file: File;
  studentId?: string;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching student coursework tasks with dynamic missing projection (SCR-SELF-03, TD-06, P5-S12).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useStudentCourseworkWorkspaceQuery(
  statusFilter?: MaybeRefOrGetter<CourseworkStatusFilter | string | null | undefined>,
  options?: StudentCourseworkQueryOptions,
) {
  return useQuery<StudentCourseworkTask[]>({
    queryKey: computed(() => {
      const resolvedFilter = toValue(statusFilter);
      return studentCourseworkKeys.workspace(resolvedFilter);
    }),
    queryFn: () => {
      const resolvedFilter = toValue(statusFilter);
      return getStudentCourseworkWorkspace(resolvedFilter ?? undefined);
    },
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutations
// ============================================================================

/**
 * Mutation hook for students submitting coursework evidence (link or file reference) (TD-07, UC-ACD-01, P5-S11).
 * Invalidates student coursework workspace query and assignment queries upon success.
 */
export function useSubmitCourseworkMutation(
  options?: StudentCourseworkMutationOptions<SubmitCourseworkResponse, SubmitCourseworkVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ assignmentId, request }: SubmitCourseworkVariables) =>
      submitCoursework(assignmentId, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: studentCourseworkKeys.all });
      queryClient.invalidateQueries({ queryKey: ['academics', 'assignments'] });
      notifySuccess(
        toast,
        'Coursework Submitted',
        `Submission received with status '${data.submissionStatus ?? data.status}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Coursework Submission Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for uploading a coursework submission file to temporary storage (TD-07, P5-S11).
 */
export function useUploadSubmissionFileMutation(
  options?: StudentCourseworkMutationOptions<FileUploadResponse, UploadSubmissionFileVariables>,
) {
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ file, studentId }: UploadSubmissionFileVariables) =>
      uploadSubmissionFile(file, studentId),
    onSuccess: (data, variables) => {
      notifySuccess(
        toast,
        'File Uploaded',
        `File '${data.originalFileName ?? data.fileName}' uploaded successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'File Upload Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
