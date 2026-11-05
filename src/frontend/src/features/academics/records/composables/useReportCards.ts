import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { reportCardsApi } from '../api/reportCardsApi';
import type {
  ApproveReportCardRequest,
  BatchCompilationSummary,
  BatchCompileReportCardsRequest,
  CompileReportCardRequest,
  ReportCard,
} from '../types';
import { reportCardKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Types
// ============================================================================

export interface ReportCardQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface ReportCardMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface ApproveReportCardVariables {
  id: string;
  request?: ApproveReportCardRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching report cards for student/parent portal or cohort view (UC-ACD-13, P8-S20).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useReportCardsQuery(
  periodIdOrParams?: MaybeRefOrGetter<string | { studentId?: string; academicPeriodId?: string } | null | undefined>,
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: ReportCardQueryOptions,
) {
  const resolvedParams = computed<{ studentId?: string; academicPeriodId?: string } | undefined>(() => {
    const firstArg = toValue(periodIdOrParams);
    if (typeof firstArg === 'object' && firstArg !== null) {
      return firstArg;
    }
    const params: { studentId?: string; academicPeriodId?: string } = {};
    if (typeof firstArg === 'string' && firstArg.length > 0) {
      params.academicPeriodId = firstArg;
    }
    const resolvedStudent = toValue(studentId);
    if (resolvedStudent) {
      params.studentId = resolvedStudent;
    }
    return Object.keys(params).length > 0 ? params : undefined;
  });

  return useQuery<ReportCard[]>({
    queryKey: computed(() => {
      const params = resolvedParams.value;
      return reportCardKeys.list(params);
    }),
    queryFn: () => {
      return reportCardsApi.getStudentReportCards(resolvedParams.value);
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
 * Reactive TanStack Query hook fetching report card details by ID.
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useReportCardDetailQuery(
  id: MaybeRefOrGetter<string | null | undefined>,
  options?: ReportCardQueryOptions,
) {
  return useQuery<ReportCard>({
    queryKey: computed(() => {
      const resolvedId = toValue(id);
      return reportCardKeys.detail(resolvedId);
    }),
    queryFn: () => {
      const resolvedId = toValue(id);
      if (!resolvedId) {
        throw new Error('Report Card ID is required to fetch details.');
      }
      return reportCardsApi.getById(resolvedId);
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
 * Mutation hook for compiling a draft report card for a student (UC-ACD-13, P5-S11).
 */
export function useCompileReportCardMutation(
  options?: ReportCardMutationOptions<ReportCard, CompileReportCardRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: CompileReportCardRequest) => reportCardsApi.compile(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: reportCardKeys.all });
      notifySuccess(
        toast,
        'Report Card Compiled',
        `Draft report card compiled for student (${data.academicPeriodLabel}).`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Report Card Compilation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for batch-compiling report cards across a section or cohort (TD-07, P5-S12).
 */
export function useBatchCompileReportCardsMutation(
  options?: ReportCardMutationOptions<BatchCompilationSummary, BatchCompileReportCardsRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: BatchCompileReportCardsRequest) =>
      reportCardsApi.batchCompile(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: reportCardKeys.all });
      notifySuccess(
        toast,
        'Batch Compilation Completed',
        `Compiled ${data.compiledCount} report card(s), ${data.skippedExistingCount} skipped existing.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Batch Compilation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for approving a report card and freezing immutable snapshot (UC-ACD-13, P5-S12).
 */
export function useApproveReportCardMutation(
  options?: ReportCardMutationOptions<ReportCard, ApproveReportCardVariables | string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ApproveReportCardVariables | string) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      const request = typeof variables === 'string' ? undefined : variables.request;
      return reportCardsApi.approve(id, request);
    },
    onSuccess: (data, variables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      queryClient.invalidateQueries({ queryKey: reportCardKeys.all });
      queryClient.invalidateQueries({ queryKey: reportCardKeys.detail(id) });
      notifySuccess(
        toast,
        'Report Card Approved',
        `Report card officially approved and certified.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Report Card Approval Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for releasing an approved report card to self-service portals (UC-ACD-13, P5-S12).
 */
export function usePublishReportCardMutation(
  options?: ReportCardMutationOptions<ReportCard, string | { id: string }>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: string | { id: string }) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      return reportCardsApi.publish(id);
    },
    onSuccess: (data, variables) => {
      const id = typeof variables === 'string' ? variables : variables.id;
      queryClient.invalidateQueries({ queryKey: reportCardKeys.all });
      queryClient.invalidateQueries({ queryKey: reportCardKeys.detail(id) });
      notifySuccess(
        toast,
        'Report Card Published',
        `Report card released to student and parent portal.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Publish Report Card Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ============================================================================
// Consolidated Composable
// ============================================================================

/**
 * Primary composable for managing report cards and certification workflow (P8-S20).
 * Bundles report card list query with compile, batchCompile, approve, and publish mutations.
 */
export function useReportCards(
  periodId?: MaybeRefOrGetter<string | null | undefined>,
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: ReportCardQueryOptions,
) {
  const reportCardsQuery = useReportCardsQuery(periodId, studentId, options);
  const compileMutation = useCompileReportCardMutation();
  const batchCompileMutation = useBatchCompileReportCardsMutation();
  const approveMutation = useApproveReportCardMutation();
  const publishMutation = usePublishReportCardMutation();

  return {
    reportCardsQuery,
    reportCards: computed(() => reportCardsQuery.data.value ?? []),
    isLoading: reportCardsQuery.isLoading,
    isPending: reportCardsQuery.isPending,
    isError: reportCardsQuery.isError,
    error: reportCardsQuery.error,
    refetch: reportCardsQuery.refetch,

    compile: compileMutation,
    batchCompile: batchCompileMutation,
    approve: approveMutation,
    publish: publishMutation,
  };
}
