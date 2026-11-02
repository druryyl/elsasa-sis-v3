import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery, useMutation, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getAmendments,
  getCorrections,
  submitCorrection,
  type CalendarAmendmentDto,
  type RecordCorrectionDto,
  type SubmitCorrectionPayload,
} from '../api/amendment.api';
import {
  amendmentKeys,
  recordCorrectionKeys,
  academicYearKeys,
  operationalPeriodKeys,
  calendarKeys,
} from './queryKeys';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export { amendmentKeys, recordCorrectionKeys };
export type { CalendarAmendmentDto, RecordCorrectionDto, SubmitCorrectionPayload };

export interface AmendmentsQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface AmendmentMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

/**
 * Reactive TanStack Query hook fetching the audit amendment timeline for a published school calendar.
 * Uses TRANSACTIONAL cache tier (staleTime: 0).
 */
export function useCalendarAmendmentsQuery(
  calendarId: MaybeRefOrGetter<string | null | undefined>,
  options?: AmendmentsQueryOptions,
) {
  return useQuery({
    queryKey: computed(() => amendmentKeys.byCalendar(toValue(calendarId) || '')),
    queryFn: () => getAmendments(toValue(calendarId)!),
    enabled: computed(() => {
      const resolvedId = toValue(calendarId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching record corrections history for an academic year context.
 */
export function useRecordCorrectionsQuery(
  academicYearId: MaybeRefOrGetter<string | null | undefined>,
  options?: AmendmentsQueryOptions,
) {
  return useQuery({
    queryKey: computed(() => recordCorrectionKeys.byAcademicYear(toValue(academicYearId) || '')),
    queryFn: () => getCorrections(toValue(academicYearId)!),
    enabled: computed(() => {
      const resolvedId = toValue(academicYearId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Mutation hook submitting an authorized operational record correction (UC-OPS-13).
 * Broad invalidation ensures corrected academic years, periods, or calendars immediately reflect updates.
 */
export function useSubmitCorrectionMutation(
  options?: AmendmentMutationOptions<RecordCorrectionDto, SubmitCorrectionPayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: SubmitCorrectionPayload) => submitCorrection(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: recordCorrectionKeys.all });
      queryClient.invalidateQueries({ queryKey: amendmentKeys.all });
      queryClient.invalidateQueries({ queryKey: academicYearKeys.all });
      queryClient.invalidateQueries({ queryKey: operationalPeriodKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      notifySuccess(
        toast,
        'Record Correction Submitted',
        `Correction for '${data.fieldName}' authorized successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Correction Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
