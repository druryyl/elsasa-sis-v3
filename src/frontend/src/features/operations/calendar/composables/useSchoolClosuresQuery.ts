import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery, useMutation, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getClosures,
  declareClosure,
  liftClosure,
  type SchoolClosureDto,
  type DeclareSchoolClosurePayload,
} from '../api/closure.api';
import { schoolClosureKeys, calendarKeys, amendmentKeys } from './queryKeys';
import { tryGetToast, notifySuccess, notifyError, type ToastOptions } from './toastNotifications';

export { schoolClosureKeys };
export type { SchoolClosureDto, DeclareSchoolClosurePayload };

export interface SchoolClosuresQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface SchoolClosureMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

/**
 * Reactive TanStack Query hook fetching all campus closures declared for an academic year.
 * Uses OPERATIONAL cache tier (5m stale, 15m gc).
 */
export function useSchoolClosuresQuery(
  academicYearId: MaybeRefOrGetter<string | null | undefined>,
  options?: SchoolClosuresQueryOptions,
) {
  return useQuery({
    queryKey: computed(() => schoolClosureKeys.byAcademicYear(toValue(academicYearId) || '')),
    queryFn: () => getClosures(toValue(academicYearId)!),
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
 * Mutation hook declaring a school closure (UC-OPS-12).
 * Automated cache invalidation pipeline guarantees that:
 * 1. Declared school closures list refetches immediately.
 * 2. Visual calendar matrix projection refetches, showing non-instructional closure markers.
 * 3. Calendar amendments timeline refetches, displaying the new versioned audit record.
 * 4. School calendars list refetches with the bumped version number.
 */
export function useDeclareSchoolClosureMutation(
  options?: SchoolClosureMutationOptions<SchoolClosureDto, DeclareSchoolClosurePayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: DeclareSchoolClosurePayload) => declareClosure(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: schoolClosureKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      queryClient.invalidateQueries({ queryKey: amendmentKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.all });
      notifySuccess(
        toast,
        'Campus Closure Declared',
        `Closure ${data.closureNumber} declared successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Declare Closure Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook lifting an active school closure.
 * Automatically triggers cache invalidation across closures, calendar matrix, and amendments.
 */
export function useLiftSchoolClosureMutation(
  options?: SchoolClosureMutationOptions<SchoolClosureDto, string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (closureId: string) => liftClosure(closureId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: schoolClosureKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.matrixAll });
      queryClient.invalidateQueries({ queryKey: amendmentKeys.all });
      queryClient.invalidateQueries({ queryKey: calendarKeys.all });
      notifySuccess(
        toast,
        'Closure Lifted',
        `Closure ${data.closureNumber} has been lifted.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Lift Closure Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
