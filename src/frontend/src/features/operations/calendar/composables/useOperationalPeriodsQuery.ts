import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery, useMutation, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getPeriods,
  createPeriod,
  updatePeriod,
  closePeriod,
  type OperationalPeriodDto,
  type CreateOperationalPeriodPayload,
  type UpdateOperationalPeriodPayload,
} from '../api/operationalPeriod.api';
import { operationalPeriodKeys, calendarKeys } from './queryKeys';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export { operationalPeriodKeys };

export interface OperationalPeriodsQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface OperationalPeriodMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

/**
 * Reactive TanStack Query hook fetching all operational periods for an academic year.
 * Uses OPERATIONAL cache tier (5m stale, 15m gc, refetch on window focus).
 */
export function useOperationalPeriodsQuery(
  academicYearId: MaybeRefOrGetter<string | null | undefined>,
  options?: OperationalPeriodsQueryOptions,
) {
  return useQuery({
    queryKey: computed(() => operationalPeriodKeys.byAcademicYear(toValue(academicYearId) || '')),
    queryFn: () => getPeriods(toValue(academicYearId)!),
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
 * Mutation hook creating a new operational period within an academic year.
 * Invalidation pipeline automatically triggers refetching of operational periods
 * and calendar matrix projections.
 */
export function useCreateOperationalPeriodMutation(
  options?: OperationalPeriodMutationOptions<
    OperationalPeriodDto,
    { academicYearId: string; payload: CreateOperationalPeriodPayload }
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
      payload: CreateOperationalPeriodPayload;
    }) => createPeriod(academicYearId, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: operationalPeriodKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      notifySuccess(
        toast,
        'Period Created',
        `Operational period '${data.periodCode}' created successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Period Creation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook updating an operational period's details.
 */
export function useUpdateOperationalPeriodMutation(
  options?: OperationalPeriodMutationOptions<
    OperationalPeriodDto,
    { id: string; payload: UpdateOperationalPeriodPayload }
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateOperationalPeriodPayload }) =>
      updatePeriod(id, payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: operationalPeriodKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      notifySuccess(
        toast,
        'Period Updated',
        `Operational period '${data.periodCode}' updated successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Period Update Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook closing an operational period.
 */
export function useCloseOperationalPeriodMutation(
  options?: OperationalPeriodMutationOptions<OperationalPeriodDto, string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (id: string) => closePeriod(id),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: operationalPeriodKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      notifySuccess(
        toast,
        'Period Closed',
        `Operational period '${data.periodCode}' has been closed.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Close Period Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
