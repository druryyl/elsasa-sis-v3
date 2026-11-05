import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { transcriptsApi } from '../api/transcriptsApi';
import type {
  GenerateInterimTranscriptRequest,
  IssueTranscriptResponse,
  Transcript,
} from '../types';
import { transcriptKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Types
// ============================================================================

export interface TranscriptQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface TranscriptMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching cumulative transcript for a student (UC-ACD-13, P8-S20).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useStudentTranscriptQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  transcriptType?: MaybeRefOrGetter<string | null | undefined>,
  options?: TranscriptQueryOptions,
) {
  return useQuery<Transcript>({
    queryKey: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const resolvedType = toValue(transcriptType);
      return transcriptKeys.byStudent(resolvedStudentId, resolvedType);
    }),
    queryFn: () => {
      const resolvedStudentId = toValue(studentId);
      if (!resolvedStudentId) {
        throw new Error('Student ID is required to fetch transcript.');
      }
      return transcriptsApi.getByStudent(
        resolvedStudentId,
        toValue(transcriptType) ?? undefined,
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

/**
 * Reactive TanStack Query hook fetching transcript details by record ID.
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useTranscriptDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: TranscriptQueryOptions,
) {
  return useQuery<Transcript>({
    queryKey: computed(() => {
      const resolvedId = toValue(id);
      return transcriptKeys.detail(resolvedId);
    }),
    queryFn: () => {
      const resolvedId = toValue(id);
      if (!resolvedId) {
        throw new Error('Transcript ID is required to fetch details.');
      }
      return transcriptsApi.getById(resolvedId);
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
 * Mutation hook for compiling a cumulative interim transcript for a student (UC-ACD-13, P5-S13).
 */
export function useGenerateTranscriptMutation(
  options?: TranscriptMutationOptions<Transcript, GenerateInterimTranscriptRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: GenerateInterimTranscriptRequest) =>
      transcriptsApi.generate(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: transcriptKeys.all });
      queryClient.invalidateQueries({
        queryKey: transcriptKeys.byStudent(variables.studentId),
      });
      notifySuccess(
        toast,
        'Transcript Generated',
        `Interim transcript compiled successfully for student.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Generate Transcript Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for officially certifying and issuing a transcript (UC-ACD-13, P5-S13).
 */
export function useIssueTranscriptMutation(
  options?: TranscriptMutationOptions<IssueTranscriptResponse, string | { id: string }>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: string | { id: string }) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      return transcriptsApi.issue(id);
    },
    onSuccess: (data, variables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      queryClient.invalidateQueries({ queryKey: transcriptKeys.all });
      queryClient.invalidateQueries({ queryKey: transcriptKeys.detail(id) });
      queryClient.invalidateQueries({
        queryKey: transcriptKeys.byStudent(data.studentId),
      });
      notifySuccess(
        toast,
        'Transcript Issued',
        `Transcript officially issued with status '${data.status}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Issue Transcript Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ============================================================================
// Consolidated Composable
// ============================================================================

/**
 * Primary composable for managing student transcripts and issuance (P8-S20).
 * Bundles transcript query with generate and issue mutations.
 */
export function useTranscripts(
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  transcriptType?: MaybeRefOrGetter<string | null | undefined>,
  options?: TranscriptQueryOptions,
) {
  const transcriptQuery = useStudentTranscriptQuery(studentId, transcriptType, options);
  const generateMutation = useGenerateTranscriptMutation();
  const issueMutation = useIssueTranscriptMutation();

  return {
    transcriptQuery,
    transcript: computed(() => transcriptQuery.data.value),
    isLoading: transcriptQuery.isLoading,
    isPending: transcriptQuery.isPending,
    isError: transcriptQuery.isError,
    error: transcriptQuery.error,
    refetch: transcriptQuery.refetch,

    generate: generateMutation,
    issue: issueMutation,
  };
}
