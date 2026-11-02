import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery, useMutation, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getCalendars,
  getActiveCalendar,
  createCalendar,
  publishCalendar,
  withdrawCalendar,
  getMatrixProjection,
  createCalendarEntry,
  deleteCalendarEntry,
  type SchoolCalendarDto,
  type CalendarEntryDto,
  type CalendarMatrixDayDto,
  type CreateCalendarPayload,
  type CreateCalendarEntryPayload,
  type MatrixProjectionParams,
} from '../api/calendar.api';
import { calendarKeys, amendmentKeys } from './queryKeys';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export { calendarKeys };

export interface CalendarQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface CalendarMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

/**
 * Reactive TanStack Query hook fetching all school calendars for an academic year.
 */
export function useCalendarsQuery(
  academicYearId: MaybeRefOrGetter<string | null | undefined>,
  options?: CalendarQueryOptions,
) {
  return useQuery({
    queryKey: computed(() => calendarKeys.byAcademicYear(toValue(academicYearId) || '')),
    queryFn: () => getCalendars(toValue(academicYearId)!),
    enabled: computed(() => {
      const resolvedId = toValue(academicYearId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching the active published calendar for an academic year.
 */
export function useActiveCalendarQuery(
  academicYearId: MaybeRefOrGetter<string | null | undefined>,
  options?: CalendarQueryOptions,
) {
  return useQuery({
    queryKey: computed(() => calendarKeys.active(toValue(academicYearId) || '')),
    queryFn: () => getActiveCalendar(toValue(academicYearId)!),
    enabled: computed(() => {
      const resolvedId = toValue(academicYearId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook computing dynamic instructional projection matrix
 * overlaid with granular exception days (holidays, staff days, closures).
 * Uses TRANSACTIONAL cache tier (staleTime: 0, always refetches).
 */
export function useCalendarMatrixQuery(
  calendarId: MaybeRefOrGetter<string | null | undefined>,
  params: MaybeRefOrGetter<MatrixProjectionParams | null | undefined>,
  options?: CalendarQueryOptions,
) {
  return useQuery({
    queryKey: computed(() =>
      calendarKeys.matrix(toValue(calendarId) || '', toValue(params) || undefined),
    ),
    queryFn: () => getMatrixProjection(toValue(calendarId)!, toValue(params)!),
    enabled: computed(() => {
      const resolvedCalendarId = toValue(calendarId);
      const resolvedParams = toValue(params);
      const hasDates = Boolean(resolvedParams?.startDate && resolvedParams?.endDate);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedCalendarId) && hasDates && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Mutation hook creating a new school calendar container.
 */
export function useCreateCalendarMutation(
  options?: CalendarMutationOptions<
    SchoolCalendarDto,
    { academicYearId: string; payload: CreateCalendarPayload }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({
      academicYearId,
      payload,
    }: {
      academicYearId: string;
      payload: CreateCalendarPayload;
    }) => createCalendar(academicYearId, payload),
    onSuccess: (data, variables) => {
      // Optimistically register the newly created calendar in the cached list so
      // the workspace reflects it immediately, even before the refetch resolves.
      queryClient.setQueryData<SchoolCalendarDto[]>(
        calendarKeys.byAcademicYear(variables.academicYearId),
        (old) => {
          const list = Array.isArray(old) ? old : [];
          if (list.some((c) => c.id === data.id)) return list;
          return [...list, data];
        },
      );
      queryClient.invalidateQueries({ queryKey: calendarKeys.all });
      notifySuccess(
        toast,
        'Calendar Created',
        `School calendar '${data.name}' created successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Calendar Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook publishing a school calendar, establishing immutable date baselines.
 * Automatically invalidates calendar containers, projections, and amendment timelines.
 */
export function usePublishCalendarMutation(
  options?: CalendarMutationOptions<SchoolCalendarDto, string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (calendarId: string) => publishCalendar(calendarId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: calendarKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      queryClient.invalidateQueries({ queryKey: amendmentKeys.all });
      notifySuccess(
        toast,
        'Calendar Published',
        `Calendar '${data.name}' (v${data.version}) has been published.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Publication Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook withdrawing a published calendar.
 */
export function useWithdrawCalendarMutation(
  options?: CalendarMutationOptions<SchoolCalendarDto, string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (calendarId: string) => withdrawCalendar(calendarId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: calendarKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      notifySuccess(
        toast,
        'Calendar Withdrawn',
        `Calendar '${data.name}' has been withdrawn.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Withdrawal Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook creating a calendar exception entry (holiday, staff day, etc.).
 * Automated invalidation pipeline immediately refetches the calendar matrix projection,
 * calendar amendment history (if published), and calendar version details.
 */
export function useCreateCalendarEntryMutation(
  options?: CalendarMutationOptions<
    CalendarEntryDto,
    { calendarId: string; payload: CreateCalendarEntryPayload }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({
      calendarId,
      payload,
    }: {
      calendarId: string;
      payload: CreateCalendarEntryPayload;
    }) => createCalendarEntry(calendarId, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      queryClient.invalidateQueries({ queryKey: amendmentKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.all });
      notifySuccess(
        toast,
        'Calendar Entry Added',
        `Entry '${data.name}' added to calendar.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Add Entry Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook deleting a calendar exception entry.
 * Automated invalidation pipeline immediately refetches the calendar matrix projection,
 * calendar amendments, and calendar details.
 */
export function useDeleteCalendarEntryMutation(
  options?: CalendarMutationOptions<void, { calendarId: string; entryId: string }>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ calendarId, entryId }: { calendarId: string; entryId: string }) =>
      deleteCalendarEntry(calendarId, entryId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      queryClient.invalidateQueries({ queryKey: amendmentKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.all });
      notifySuccess(
        toast,
        'Calendar Entry Removed',
        'The calendar entry has been deleted.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Delete Entry Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
